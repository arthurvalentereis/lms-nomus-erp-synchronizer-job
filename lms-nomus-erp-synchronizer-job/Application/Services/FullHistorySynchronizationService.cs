using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Carga inicial do histórico: Nomus página a página (até vazio) → buffer → Letmesee em lotes de 1000.
/// </summary>
public sealed class FullHistorySynchronizationService : IFullHistorySynchronizationService
{
    public const int LetmeseeBatchSize = 1000;

    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly ILogger<FullHistorySynchronizationService> _logger;

    public FullHistorySynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        ILogger<FullHistorySynchronizationService> logger)
    {
        _nomusClientFactory = nomusClientFactory;
        _letmeseeService = letmeseeService;
        _logger = logger;
    }

    public async Task ExecuteAsync(
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
            ["SyncMode"] = "FullHistory"
        });

        _logger.LogInformation(
            "Iniciando carga de histórico completo do cliente {UserGroupId} (Nomus: página a página até vazio; Letmesee: lotes de {BatchSize})",
            userGroupId,
            LetmeseeBatchSize);

        var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

        //var customersSent = await SyncCustomersAsync(
            //nomusClient, userGroupId, userCompanyId, cancellationToken);

        var invoicesSent = await SyncInvoicesAsync(
            nomusClient, userGroupId, creditorDocument, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Carga de histórico do cliente {UserGroupId} concluída. Customers: {CustomersSent}, Invoices: {InvoicesSent}, Duração: {Duration}ms",
            userGroupId,
            //customersSent,
            invoicesSent,
            duration.TotalMilliseconds);
    }

    private async Task<int> SyncCustomersAsync(
        INomusClient nomusClient,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        var filtro = "ativo=true";
        var encoded = Uri.EscapeDataString(filtro);
        var url = $"rest/clientes?query={encoded}";

        var sendBuffer = new LetmeseePagedBuffer<RequestCustomerDto>(
            LetmeseeBatchSize,
            batch => _letmeseeService.SendCustomerAsync(batch, cancellationToken));

        var pagina = 0;

        await foreach (var page in nomusClient.StreamPagesAsync<CustomerDto>(url, cancellationToken))
        {
            pagina++;
            var mapped = page
                .Select(Infrastructure.Nomus.Mappers.CustomerMapper.ToDomain)
                .Select(c => Mappers.CustomerMapper.ToCustomerDto(c, userGroupId, userCompanyId))
                .ToList();

            sendBuffer.AddRange(mapped);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogDebug(
                "Customers página {Pagina}: {Count} itens (pendente no buffer: {Pending})",
                pagina,
                mapped.Count,
                sendBuffer.PendingCount);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de customers do cliente {UserGroupId}: {TotalSent} enviados em {Paginas} páginas Nomus",
            userGroupId,
            sendBuffer.TotalSent,
            pagina);

        return sendBuffer.TotalSent;
    }

    /// <summary>
    /// Contas a receber guiam a paginação; recebimentos/boletos usam o mesmo índice de página.
    /// Encerra quando a página de contas vier vazia (fim do histórico).
    /// </summary>
    private async Task<int> SyncInvoicesAsync(
        INomusClient nomusClient,
        long userGroupId,
        string creditorDocument,
        CancellationToken cancellationToken)
    {
        const string contasUrl = "rest/contasReceber";
        const string recebimentosUrl = "rest/recebimentos";
        //const string boletosUrl = "rest/boletos";

        var sendBuffer = new LetmeseePagedBuffer<RequestInvoiceDto>(
            LetmeseeBatchSize,
            batch => _letmeseeService.SendInvoicesAsync(batch, cancellationToken));

        var pagina = 0;

        while (true)
        {
            pagina++;
            var contasDto = await nomusClient.GetPageAsync<ContaReceberDto>(contasUrl, pagina, cancellationToken);

            if (contasDto.Count == 0)
            {
                _logger.LogDebug(
                    "Contas página {Pagina} vazia — fim do histórico de invoices (cliente {UserGroupId})",
                    pagina,
                    userGroupId);
                break;
            }

            var recebimentosDto = await nomusClient.GetPageAsync<RecebimentoDto>(recebimentosUrl, pagina, cancellationToken);
            //var boletosDto = await nomusClient.GetPageAsync<BoletoDto>(boletosUrl, pagina, cancellationToken);

            var contas = contasDto
                .Select(ContaReceberMapper.ToDomain)
                .ToList();
            var recebimentos = recebimentosDto
                .Select(RecebimentoMapper.ToDomain)
                .ToList();
            var boletos = new List<Boleto>();

            var pageInvoices = InvoiceMapper.ToInvoiceDtos(
                contas,
                boletos,
                recebimentos,
                userGroupId,
                creditorDocument).ToList();

            sendBuffer.AddRange(pageInvoices);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogDebug(
                "Invoices página {Pagina} (cliente {UserGroupId}): {Contas} contas → {Invoices} invoices (pendente: {Pending})",
                pagina,
                userGroupId,
                contas.Count,
                pageInvoices.Count,
                sendBuffer.PendingCount);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de invoices do cliente {UserGroupId}: {TotalSent} enviadas em {Paginas} páginas Nomus (lotes de {BatchSize})",
            userGroupId,
            sendBuffer.TotalSent,
            pagina > 0 ? pagina - 1 : 0,
            LetmeseeBatchSize);

        return sendBuffer.TotalSent;
    }
}
