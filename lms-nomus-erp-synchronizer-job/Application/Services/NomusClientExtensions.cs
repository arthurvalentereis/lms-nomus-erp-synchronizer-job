using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

internal static class NomusClientExtensions
{
    /// <summary>Agrega todas as páginas em uma lista (uso típico: sync D-1 com poucas páginas).</summary>
    public static async Task<List<TDomain>> CollectAllPagesAsync<TDto, TDomain>(
        this INomusClient client,
        string url,
        Func<TDto, TDomain> map,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TDomain>();

        await foreach (var page in client.StreamPagesAsync<TDto>(url, cancellationToken))
            result.AddRange(page.Select(map));

        return result;
    }
}
