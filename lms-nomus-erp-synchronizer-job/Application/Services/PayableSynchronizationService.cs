using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Sincronização incremental (D-1) de contas a pagar: Nomus com filtro de data → Letmesee.
/// Não agenda o orquestrador (responsabilidade do fluxo de contas a receber, compartilha o mesmo ciclo).
/// </summary>
public class PayableSynchronizationService : IPayableSynchronizationService
{
    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly IFullHistoryPayableSynchronizationService _fullHistorySync;
    private readonly ILogger<PayableSynchronizationService> _logger;

    public PayableSynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        IFullHistoryPayableSynchronizationService fullHistorySync,
        ILogger<PayableSynchronizationService> logger)
    {
        _nomusClientFactory = nomusClientFactory;
        _letmeseeService = letmeseeService;
        _fullHistorySync = fullHistorySync;
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
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserGroupId"] = userGroupId,
            ["SyncType"] = "ContasAPagar"
        });

        _logger.LogInformation("Iniciando sincronização D-1 de contas a pagar do cliente {UserGroupId}", userGroupId);

        var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

        var suppliersTask = FetchD1Async<PessoaDto, Supplier>(
            nomusClient, userGroupId,
            BuildD1Url("rest/pessoas", "ativo=true;dataModificacao>={0}", "s"),
            PessoaMapper.ToDomain,
            "suppliers/pessoas",
            cancellationToken);

        var pagamentosTask = FetchD1Async<PagamentoDto, Pagamento>(
            nomusClient, userGroupId,
            BuildD1Url("rest/pagamentos", "dataModificacao>={0}", "s"),
            PagamentoMapper.ToDomain,
            "pagamentos",
            cancellationToken);

        var contasTask = FetchD1Async<ContaPagarDto, ContaPagar>(
            nomusClient, userGroupId,
            BuildD1Url("rest/contasPagar", "dataModificacao>{0}", "s"),
            ContaPagarMapper.ToDomain,
            "contas a pagar",
            cancellationToken);

        await Task.WhenAll(suppliersTask, pagamentosTask, contasTask);

        var pagamentos = await pagamentosTask;
        var contasPagar = await contasTask;
        var suppliers = await suppliersTask;

        // Mesmos critérios do fluxo de contas a receber (baixa efetiva)
        pagamentos = pagamentos.Where(p => p.BaixaContaPagar == true).ToList();
        contasPagar = contasPagar.Where(c => c.Status == true).ToList();

        // Cache D-1 + GET por idPessoa das faturas (contasPagar não retorna cnpjPessoa)
        var documentCache = SupplierDocumentResolver.BuildDocumentCache(suppliers);
        var missingSuppliers = await SupplierDocumentResolver.FetchMissingPessoasAsync(
            nomusClient,
            contasPagar.Select(c => c.IdPessoa),
            documentCache,
            _logger,
            cancellationToken);

        if (missingSuppliers.Count > 0)
        {
            suppliers = MergeSuppliersById(suppliers, missingSuppliers);
            foreach (var kv in SupplierDocumentResolver.BuildDocumentCache(missingSuppliers))
                documentCache[kv.Key] = kv.Value;
        }

        var hydrated = SupplierDocumentResolver.HydrateContasFromCache(contasPagar, documentCache);
        var withoutDoc = contasPagar.Count(c => string.IsNullOrWhiteSpace(c.CnpjCpfPessoa));

        _logger.LogInformation(
            "Nomus D-1 contas a pagar cliente {UserGroupId}: Pagamentos {PagamentoCount}, Contas {ContaCount}, Suppliers {SupplierCount}, Contas com documento {HydratedCount}, sem documento {WithoutDocCount}",
            userGroupId,
            pagamentos.Count,
            contasPagar.Count,
            suppliers.Count,
            hydrated,
            withoutDoc);

        // Supplier antes da invoice para o Leader já ter o cadastro ao vincular por documento
        await SendSuppliersAsync(suppliers, userGroupId, userCompanyId, cancellationToken);
        await SendSupplierInvoicesAsync(pagamentos, contasPagar, userGroupId, creditorDocument, cancellationToken);

        _logger.LogInformation(
            "Sincronização D-1 de contas a pagar do cliente {UserGroupId} concluída em {Duration}ms",
            userGroupId,
            (DateTime.UtcNow - startTime).TotalMilliseconds);
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

    private static List<Supplier> MergeSuppliersById(List<Supplier> existing, List<Supplier> extras)
    {
        var byId = existing
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var supplier in extras)
            byId[supplier.Id] = supplier;

        return byId.Values.ToList();
    }

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
        _logger.LogDebug("Buscando {Resource} (D-1 AP) do cliente {UserGroupId}", resourceLabel, userGroupId);

        var items = await nomusClient.CollectAllPagesAsync(url, map, cancellationToken);

        _logger.LogInformation(
            "Total {Resource} (D-1 AP) cliente {UserGroupId}: {Count}",
            resourceLabel,
            userGroupId,
            items.Count);

        return items;
    }

    private async Task SendSupplierInvoicesAsync(
        List<Pagamento> pagamentos,
        List<ContaPagar> contasPagar,
        long userGroupId,
        string creditorDocument,
        CancellationToken cancellationToken)
    {
        var invoices = SupplierInvoiceMapper.ToInvoiceDtos(
            contasPagar,
            pagamentos,
            userGroupId,
            creditorDocument).ToList();

        if (invoices.Count == 0)
        {
            _logger.LogInformation("Nenhuma supplier invoice para enviar (cliente {UserGroupId})", userGroupId);
            return;
        }

        _logger.LogInformation(
            "Enviando {Count} supplier invoices ao Letmesee (cliente {UserGroupId})",
            invoices.Count,
            userGroupId);
        await _letmeseeService.SendSupplierInvoicesAsync(invoices, cancellationToken);
    }

    private async Task SendSuppliersAsync(
        List<Supplier> suppliers,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        var dtos = suppliers
            .Select(s => SupplierMapper.ToSupplierDto(s, userGroupId, userCompanyId))
            .ToList();

        if (dtos.Count == 0)
        {
            _logger.LogInformation("Nenhum supplier para enviar (cliente {UserGroupId})", userGroupId);
            return;
        }

        _logger.LogInformation("Enviando {Count} suppliers ao Letmesee (cliente {UserGroupId})", dtos.Count, userGroupId);
        await _letmeseeService.SendSuppliersAsync(dtos, cancellationToken);
    }
}
