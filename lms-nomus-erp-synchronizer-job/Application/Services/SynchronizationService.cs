using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Sincronização incremental (D-1): Nomus com filtro de data → Letmesee em um envio por tipo.
/// </summary>
public class SynchronizationService : ISynchronizationService
{
    private const int MinutosAteProximoCicloSucesso = 5;
    private const int MinutosAteProximoCicloErro = 15;

    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly IFullHistorySynchronizationService _fullHistorySync;
    private readonly IOrchestratorNextRunScheduler _orchestratorNextRun;
    private readonly ILogger<SynchronizationService> _logger;

    public SynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        IFullHistorySynchronizationService fullHistorySync,
        IOrchestratorNextRunScheduler orchestratorNextRun,
        ILogger<SynchronizationService> logger)
    {
        _nomusClientFactory = nomusClientFactory;
        _letmeseeService = letmeseeService;
        _fullHistorySync = fullHistorySync;
        _orchestratorNextRun = orchestratorNextRun;
        _logger = logger;
    }

    public async Task SynchronizeClienteAsync(
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        string hashToken,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["UserGroupId"] = userGroupId });

        _logger.LogInformation("Iniciando sincronização D-1 do cliente {UserGroupId}", userGroupId);

        try
        {
            var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

            var customerTask = FetchD1Async<CustomerDto, Customer>(
                nomusClient, userGroupId,
                BuildD1Url("rest/clientes", "ativo=true;dataModificacao>={0}", "s"),
                Infrastructure.Nomus.Mappers.CustomerMapper.ToDomain,
                "customers",
                cancellationToken);

            var boletosTask = Task.FromResult(new List<Boleto>());
            var recebimentosTask = FetchD1Async<RecebimentoDto, Recebimento>(
                nomusClient, userGroupId,
                BuildD1Url("rest/recebimentos", "dataModificacao>={0}", "s"),
                RecebimentoMapper.ToDomain,
                "recebimentos",
                cancellationToken);
            var contasTask = FetchD1Async<ContaReceberDto, ContaReceber>(
                nomusClient, userGroupId,
                BuildD1Url("rest/contasReceber", "dataModificacao>{0}", "s"),
                ContaReceberMapper.ToDomain,
                "contas a receber",
                cancellationToken);

            await Task.WhenAll(customerTask, boletosTask, recebimentosTask, contasTask);

            var boletos = await boletosTask;
            var recebimentos = await recebimentosTask;
            var contasReceber = await contasTask;
            var customers = await customerTask;

            recebimentos = recebimentos.Where(r => r.BaixaContaReceber == true).ToList();
            contasReceber = contasReceber.Where(c => c.Status == true).ToList();

            _logger.LogInformation(
                "Nomus D-1 cliente {UserGroupId}: Boletos {BoletoCount}, Recebimentos {RecebimentoCount}, Contas {ContaCount}, Customers {CustomerCount}",
                userGroupId, boletos.Count, recebimentos.Count, contasReceber.Count, customers.Count);

            await SendInvoicesAsync(boletos, recebimentos, contasReceber, userGroupId, creditorDocument, cancellationToken);
            await SendCustomersAsync(customers, userGroupId, userCompanyId, cancellationToken);

            _logger.LogInformation(
                "Sincronização D-1 do cliente {UserGroupId} concluída em {Duration}ms",
                userGroupId,
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            _orchestratorNextRun.ScheduleNext(TimeSpan.FromMinutes(MinutosAteProximoCicloSucesso));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Sincronização D-1 do cliente {UserGroupId} falhou; próximo ciclo em {Minutos} min",
                userGroupId,
                MinutosAteProximoCicloErro);
            _orchestratorNextRun.ScheduleNext(TimeSpan.FromMinutes(MinutosAteProximoCicloErro));
            throw;
        }
    }

    public Task SynchronizeAllFilesClienteAsync(
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        string hashToken,
        string baseUrl,
        CancellationToken cancellationToken = default)
        => _fullHistorySync.ExecuteAsync(
            userGroupId,
            userCompanyId,
            creditorDocument,
            hashToken,
            baseUrl,
            cancellationToken);

    private static string BuildD1Url(string path, string filtroTemplate, string dateFormat)
    {
        var dataFiltro = DateTime.Now.AddDays(-1).ToString(dateFormat);
        var filtro = string.Format(filtroTemplate, dataFiltro);
        return $"{path}?query={Uri.EscapeDataString(filtro)}";
    }

    private async Task<List<TDomain>> FetchD1Async<TDto, TDomain>(
        INomusClient nomusClient,
        long userGroupId,
        string url,
        Func<TDto, TDomain> map,
        string resourceLabel,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Buscando {Resource} (D-1) do cliente {UserGroupId}", resourceLabel, userGroupId);

        var items = await nomusClient.CollectAllPagesAsync(url, map, cancellationToken);

        _logger.LogInformation(
            "Total {Resource} (D-1) cliente {UserGroupId}: {Count}",
            resourceLabel,
            userGroupId,
            items.Count);

        return items;
    }

    private async Task SendInvoicesAsync(
        List<Boleto> boletos,
        List<Recebimento> recebimentos,
        List<ContaReceber> contasReceber,
        long userGroupId,
        string creditorDocument,
        CancellationToken cancellationToken)
    {
        var invoices = InvoiceMapper.ToInvoiceDtos(
            contasReceber,
            boletos,
            recebimentos,
            userGroupId,
            creditorDocument).ToList();

        if (invoices.Count == 0)
        {
            _logger.LogInformation("Nenhuma invoice para enviar (cliente {UserGroupId})", userGroupId);
            return;
        }

        _logger.LogInformation("Enviando {Count} invoices ao Letmesee (cliente {UserGroupId})", invoices.Count, userGroupId);
        await _letmeseeService.SendInvoicesAsync(invoices, cancellationToken);
    }

    private async Task SendCustomersAsync(
        List<Customer> customers,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        var dtos = customers
            .Select(c => Mappers.CustomerMapper.ToCustomerDto(c, userGroupId, userCompanyId))
            .ToList();

        if (dtos.Count == 0)
        {
            _logger.LogInformation("Nenhum customer para enviar (cliente {UserGroupId})", userGroupId);
            return;
        }

        _logger.LogInformation("Enviando {Count} customers ao Letmesee (cliente {UserGroupId})", dtos.Count, userGroupId);
        await _letmeseeService.SendCustomerAsync(dtos, cancellationToken);
    }
}
