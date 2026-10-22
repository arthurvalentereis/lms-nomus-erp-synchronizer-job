using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Resolve CNPJ/CPF do fornecedor a partir de <c>idPessoa</c> (contas a pagar não retornam cnpjPessoa).
/// </summary>
internal static class SupplierDocumentResolver
{
    /// <summary>
    /// Monta cache idPessoa → documento limpo a partir dos suppliers já carregados.
    /// </summary>
    public static Dictionary<long, string> BuildDocumentCache(IEnumerable<Supplier> suppliers)
    {
        var cache = new Dictionary<long, string>();

        foreach (var supplier in suppliers)
            cache[supplier.Id] = InvoiceMapper.LimparDocumento(supplier.Cnpj ?? string.Empty);

        return cache;
    }

    /// <summary>
    /// Preenche <see cref="ContaPagar.CnpjCpfPessoa"/> a partir do cache de documentos.
    /// </summary>
    public static int HydrateContasFromCache(
        IEnumerable<ContaPagar> contas,
        IReadOnlyDictionary<long, string> documentByPessoaId)
    {
        var hydrated = 0;

        foreach (var conta in contas)
        {
            if (documentByPessoaId.TryGetValue(conta.IdPessoa, out var doc) && !string.IsNullOrEmpty(doc))
            {
                conta.CnpjCpfPessoa = doc;
                hydrated++;
            }
        }

        return hydrated;
    }

    /// <summary>
    /// Para ids de pessoa das faturas que não estão no cache, busca <c>rest/pessoas?query=id=N</c>
    /// e devolve os suppliers encontrados (para enriquecer cache e envio ao Leader).
    /// </summary>
    public static async Task<List<Supplier>> FetchMissingPessoasAsync(
        INomusClient nomusClient,
        IEnumerable<int> idPessoas,
        IDictionary<long, string> documentByPessoaId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var missingIds = idPessoas
            .Distinct()
            .Where(id => id > 0 && !documentByPessoaId.ContainsKey(id))
            .ToList();

        if (missingIds.Count == 0)
            return [];

        logger.LogInformation(
            "Resolvendo documento de {Count} pessoas ausentes no cache (GET rest/pessoas por id)",
            missingIds.Count);

        var fetched = new List<Supplier>(missingIds.Count);

        foreach (var id in missingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filtro = $"id={id}";
            var url = $"rest/pessoas?query={Uri.EscapeDataString(filtro)}";

            try
            {
                var page = await nomusClient.GetPageAsync<PessoaDto>(url, 1, cancellationToken);
                var dto = page.FirstOrDefault(p => p.Id == id) ?? page.FirstOrDefault();
                if (dto == null)
                {
                    logger.LogWarning("Pessoa id={IdPessoa} não encontrada na Nomus", id);
                    documentByPessoaId[id] = string.Empty;
                    continue;
                }

                var supplier = PessoaMapper.ToDomain(dto);
                var doc = InvoiceMapper.LimparDocumento(supplier.Cnpj ?? string.Empty);
                if (string.IsNullOrEmpty(doc))
                {
                    logger.LogWarning(
                        "Pessoa id={IdPessoa} ({Nome}) retornou sem CNPJ/CPF",
                        id,
                        supplier.Nome);
                }

                fetched.Add(supplier);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao buscar pessoa id={IdPessoa} na Nomus", id);
            }
        }

        logger.LogInformation(
            "Fetch por id concluído: {Fetched}/{Missing} pessoas resolvidas",
            fetched.Count,
            missingIds.Count);

        return fetched;
    }

    /// <summary>
    /// Incorpora pessoas resolvidas no cache e marca ids já consultados (evita GET repetido).
    /// </summary>
    public static void MergeIntoDocumentCache(
        IDictionary<long, string> cache,
        IEnumerable<Supplier> suppliers,
        IEnumerable<int>? resolvedIds = null)
    {
        foreach (var kv in BuildDocumentCache(suppliers))
            cache[kv.Key] = kv.Value;

        if (resolvedIds == null)
            return;

        foreach (var id in resolvedIds.Where(id => id > 0))
        {
            if (!cache.ContainsKey(id))
                cache[id] = string.Empty;
        }
    }
}
