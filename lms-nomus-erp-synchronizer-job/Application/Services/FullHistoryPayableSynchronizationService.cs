using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.Configuration;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;
using Microsoft.Extensions.Options;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Carga inicial do histórico de contas a pagar: Nomus página a página → buffer → Letmesee em lotes configuráveis.
/// Pessoas viram dicionário idPessoa → documento; invoices hidratam BuyerDocument por esse cache.
/// idPessoa ausente no cache é buscado em rest/pessoas e enviado ao Leader antes da invoice da página.
/// </summary>
public sealed class FullHistoryPayableSynchronizationService : IFullHistoryPayableSynchronizationService
{
    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly ILogger<FullHistoryPayableSynchronizationService> _logger;
    private readonly int _batchSize;

    public FullHistoryPayableSynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        IOptions<LetmeseeOptions> letmeseeOptions,
        ILogger<FullHistoryPayableSynchronizationService> logger)
    {
        _nomusClientFactory = nomusClientFactory;
        _letmeseeService = letmeseeService;
        _logger = logger;
        _batchSize = letmeseeOptions.Value.BatchSize > 0
            ? letmeseeOptions.Value.BatchSize
            : 1000;
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
            _batchSize);

        var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

        var (suppliersSent, documentCache) = await SyncSuppliersAsync(
            nomusClient, userGroupId, userCompanyId, cancellationToken);

        var invoicesSent = await SyncSupplierInvoicesAsync(
            nomusClient, userGroupId, userCompanyId, creditorDocument, documentCache, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Carga de histórico AP do cliente {UserGroupId} concluída. Suppliers: {SuppliersSent}, SupplierInvoices: {InvoicesSent}, Duração: {Duration}ms",
            userGroupId,
            suppliersSent,
            invoicesSent,
            duration.TotalMilliseconds);
    }

    private async Task<(int TotalSent, Dictionary<long, string> DocumentCache)> SyncSuppliersAsync(
        INomusClient nomusClient,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        var filtro = "ativo=true";
        var encoded = Uri.EscapeDataString(filtro);
        var url = $"rest/pessoas?query={encoded}";

        var sendBuffer = new LetmeseePagedBuffer<RequestCustomerDto>(
            _batchSize,
            batch => _letmeseeService.SendSuppliersAsync(batch, cancellationToken));

        var documentCache = new Dictionary<long, string>();
        var pagina = 0;

        await foreach (var page in nomusClient.StreamPagesAsync<PessoaDto>(url, cancellationToken))
        {
            pagina++;
            var suppliers = page.Select(PessoaMapper.ToDomain).ToList();

            foreach (var kv in SupplierDocumentResolver.BuildDocumentCache(suppliers))
                documentCache[kv.Key] = kv.Value;

            var mapped = suppliers
                .Select(s => SupplierMapper.ToSupplierDto(s, userGroupId, userCompanyId))
                .ToList();

            sendBuffer.AddRange(mapped);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogInformation(
                "Suppliers página {Pagina}: {Count} enviados ao buffer (cache docs: {CacheCount}) cliente {UserGroupId}",
                pagina,
                mapped.Count,
                documentCache.Count,
                userGroupId);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de suppliers do cliente {UserGroupId}: {TotalSent} enviados em {Paginas} páginas Nomus (cache documentos: {CacheCount})",
            userGroupId,
            sendBuffer.TotalSent,
            pagina,
            documentCache.Count);

        return (sendBuffer.TotalSent, documentCache);
    }

    private async Task<int> SyncSupplierInvoicesAsync(
        INomusClient nomusClient,
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        Dictionary<long, string> documentCache,
        CancellationToken cancellationToken)
    {
        const string contasUrl = "rest/contasPagar";
        const string pagamentosUrl = "rest/pagamentos";

        var sendBuffer = new LetmeseePagedBuffer<RequestInvoiceDto>(
            _batchSize,
            batch => _letmeseeService.SendSupplierInvoicesAsync(batch, cancellationToken));

        var pagina = 0;
        var hydratedTotal = 0;
        var withoutDocTotal = 0;
        var missingSuppliersSent = 0;

        while (true)
        {
            pagina++;
            var contasDto = await nomusClient.GetPageAsync<ContaPagarDto>(contasUrl, pagina, cancellationToken);

            if (contasDto.Count == 0)
            {
                _logger.LogInformation(
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

            var pagePessoaIds = contas.Select(c => c.IdPessoa).ToList();
            var missingSuppliers = await SupplierDocumentResolver.FetchMissingPessoasAsync(
                nomusClient,
                pagePessoaIds,
                documentCache,
                _logger,
                cancellationToken);

            if (missingSuppliers.Count > 0)
            {
                var mapped = missingSuppliers
                    .Select(s => SupplierMapper.ToSupplierDto(s, userGroupId, userCompanyId))
                    .ToList();
                await _letmeseeService.SendSuppliersAsync(mapped, cancellationToken);
                missingSuppliersSent += mapped.Count;
            }

            SupplierDocumentResolver.MergeIntoDocumentCache(documentCache, missingSuppliers);

            hydratedTotal += SupplierDocumentResolver.HydrateContasFromCache(contas, documentCache);
            withoutDocTotal += contas.Count(c => string.IsNullOrWhiteSpace(c.CnpjCpfPessoa));

            var pageInvoices = SupplierInvoiceMapper.ToInvoiceDtos(
                contas,
                pagamentos,
                userGroupId,
                creditorDocument).ToList();

            sendBuffer.AddRange(pageInvoices);
            await sendBuffer.FlushPendingAsync(cancellationToken);

            _logger.LogInformation(
                "Supplier invoices página {Pagina} (cliente {UserGroupId}): {Contas} contas → {Invoices} invoices (hidratadas {HydratedPage}, sem doc acumulado {WithoutDoc}, pessoas extra {MissingPage})",
                pagina,
                userGroupId,
                contas.Count,
                pageInvoices.Count,
                contas.Count(c => !string.IsNullOrWhiteSpace(c.CnpjCpfPessoa)),
                withoutDocTotal,
                missingSuppliers.Count);
        }

        await sendBuffer.FlushRemainderAsync(cancellationToken);

        _logger.LogInformation(
            "Histórico de supplier invoices do cliente {UserGroupId}: {TotalSent} enviadas em {Paginas} páginas Nomus (lotes de {BatchSize}); documentos hidratados {Hydrated}, sem documento {WithoutDoc}, pessoas extra enviadas {MissingSent}",
            userGroupId,
            sendBuffer.TotalSent,
            pagina > 0 ? pagina - 1 : 0,
            _batchSize,
            hydratedTotal,
            withoutDocTotal,
            missingSuppliersSent);

        return sendBuffer.TotalSent;
    }
}
