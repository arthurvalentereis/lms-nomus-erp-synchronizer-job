using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Carga inicial do histórico de contas a pagar: Nomus página a página → buffer → Letmesee em lotes de 1000.
/// </summary>
public sealed class FullHistoryPayableSynchronizationService : IFullHistoryPayableSynchronizationService
{
    public const int LetmeseeBatchSize = 1000;

    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly ILogger<FullHistoryPayableSynchronizationService> _logger;

    public FullHistoryPayableSynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        ILogger<FullHistoryPayableSynchronizationService> logger)
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
            ["SyncMode"] = "FullHistoryPayable"
        });

        _logger.LogInformation(
            "Iniciando carga de histórico completo de contas a pagar do cliente {UserGroupId} (Letmesee: lotes de {BatchSize})",
            userGroupId,
            LetmeseeBatchSize);

        var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

        var suppliersSent = await SyncSuppliersAsync(
            nomusClient, userGroupId, userCompanyId, cancellationToken);

        var invoicesSent = await SyncSupplierInvoicesAsync(
            nomusClient, userGroupId, creditorDocument, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Carga de histórico AP do cliente {UserGroupId} concluída. Suppliers: {SuppliersSent}, SupplierInvoices: {InvoicesSent}, Duração: {Duration}ms",
            userGroupId,
            suppliersSent,
            invoicesSent,
            duration.TotalMilliseconds);
    }

    private async Task<int> SyncSuppliersAsync(
        INomusClient nomusClient,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        var filtro = "ativo=true";
        var encoded = Uri.EscapeDataString(filtro);
        var url = $"rest/pessoas?query={encoded}";

        var sendBuffer = new LetmeseePagedBuffer<RequestCustomerDto>(
            LetmeseeBatchSize,
            batch => _letmeseeService.SendSuppliersAsync(batch, cancellationToken));

        var pagina = 0;

        await foreach (var page in nomusClient.StreamPagesAsync<PessoaDto>(url, cancellationToken))
        {
            pagina++;
            var mapped = page
                .Select(PessoaMapper.ToDomain)
                .Select(s => SupplierMapper.ToSupplierDto(s, userGroupId, userCompanyId))
                .ToList();

            sendBuffer.AddRange(mapped);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogDebug(
                "Suppliers página {Pagina}: {Count} itens (pendente no buffer: {Pending})",
                pagina,
                mapped.Count,
                sendBuffer.PendingCount);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de suppliers do cliente {UserGroupId}: {TotalSent} enviados em {Paginas} páginas Nomus",
            userGroupId,
            sendBuffer.TotalSent,
            pagina);

        return sendBuffer.TotalSent;
    }

    /// <summary>
    /// Contas a pagar guiam a paginação; pagamentos usam o mesmo índice de página.
    /// Encerra quando a página de contas vier vazia.
    /// </summary>
    private async Task<int> SyncSupplierInvoicesAsync(
        INomusClient nomusClient,
        long userGroupId,
        string creditorDocument,
        CancellationToken cancellationToken)
    {
        const string contasUrl = "rest/contasPagar";
        const string pagamentosUrl = "rest/pagamentos";

        var sendBuffer = new LetmeseePagedBuffer<RequestInvoiceDto>(
            LetmeseeBatchSize,
            batch => _letmeseeService.SendSupplierInvoicesAsync(batch, cancellationToken));

        var pagina = 0;

        while (true)
        {
            pagina++;
            var contasDto = await nomusClient.GetPageAsync<ContaPagarDto>(contasUrl, pagina, cancellationToken);

            if (contasDto.Count == 0)
            {
                _logger.LogDebug(
                    "Contas a pagar página {Pagina} vazia — fim do histórico de supplier invoices (cliente {UserGroupId})",
                    pagina,
                    userGroupId);
                break;
            }

            var pagamentosDto = await nomusClient.GetPageAsync<PagamentoDto>(pagamentosUrl, pagina, cancellationToken);

            var contas = contasDto
                .Select(ContaPagarMapper.ToDomain)
                .ToList();
            var pagamentos = pagamentosDto
                .Select(PagamentoMapper.ToDomain)
                .ToList();

            var pageInvoices = SupplierInvoiceMapper.ToInvoiceDtos(
                contas,
                pagamentos,
                userGroupId,
                creditorDocument).ToList();

            sendBuffer.AddRange(pageInvoices);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogDebug(
                "Supplier invoices página {Pagina} (cliente {UserGroupId}): {Contas} contas → {Invoices} invoices (pendente: {Pending})",
                pagina,
                userGroupId,
                contas.Count,
                pageInvoices.Count,
                sendBuffer.PendingCount);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de supplier invoices do cliente {UserGroupId}: {TotalSent} enviadas em {Paginas} páginas Nomus (lotes de {BatchSize})",
            userGroupId,
            sendBuffer.TotalSent,
            pagina > 0 ? pagina - 1 : 0,
            LetmeseeBatchSize);

        return sendBuffer.TotalSent;
    }
}
