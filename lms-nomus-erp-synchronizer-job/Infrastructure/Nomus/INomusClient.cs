namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Cliente para integração com a API REST do ERP Nomus (paginação via <c>?pagina=N</c>).
/// </summary>
public interface INomusClient
{
    /// <summary>Uma página (<c>?pagina=N</c>).</summary>
    Task<IReadOnlyList<TItem>> GetPageAsync<TItem>(string url, int pagina, CancellationToken cancellationToken = default);

    /// <summary>Páginas 1, 2, 3… até lista vazia ou nula.</summary>
    IAsyncEnumerable<IReadOnlyList<TItem>> StreamPagesAsync<TItem>(string url, CancellationToken cancellationToken = default);
}
