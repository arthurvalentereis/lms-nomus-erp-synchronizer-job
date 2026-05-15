using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;
using Microsoft.Extensions.Options;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Cliente HTTP para a API REST do Nomus.
/// </summary>
public class NomusClient : INomusClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NomusClient> _logger;
    private readonly NomusOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ParametroPagina = "pagina";

    public NomusClient(
        HttpClient httpClient,
        IOptions<NomusOptions> options,
        ILogger<NomusClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (!_httpClient.DefaultRequestHeaders.Accept.Contains(
                new MediaTypeWithQualityHeaderValue("application/json")))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public Task<IReadOnlyList<TItem>> GetPageAsync<TItem>(string url, int pagina, CancellationToken cancellationToken = default)
        => FetchPageAsync<TItem>(url, pagina, cancellationToken);

    public async IAsyncEnumerable<IReadOnlyList<TItem>> StreamPagesAsync<TItem>(
        string url,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pagina = 1;
        var safetyLimit = _options.MaxPaginasSafetyLimit;

        while (true)
        {
            if (safetyLimit > 0 && pagina > safetyLimit)
            {
                _logger.LogWarning(
                    "MaxPaginasSafetyLimit ({Limit}) atingido em {Url}.",
                    safetyLimit,
                    url);
                yield break;
            }

            var dados = await FetchPageAsync<TItem>(url, pagina, cancellationToken);
            if (dados.Count == 0)
            {
                _logger.LogDebug("Página {Pagina} de {Url} vazia — fim da paginação.", pagina, url);
                yield break;
            }

            _logger.LogDebug("Página {Pagina} de {Url}: {Count} itens.", pagina, url, dados.Count);
            yield return dados;
            pagina++;
        }
    }

    private async Task<IReadOnlyList<TItem>> FetchPageAsync<TItem>(
        string url,
        int pagina,
        CancellationToken cancellationToken)
    {
        var separator = url.Contains('?') ? "&" : "?";
        var endpoint = $"{url}{separator}{ParametroPagina}={pagina}";

        try
        {
            var response = await SendWithRetryAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var dados = await JsonSerializer.DeserializeAsync<List<TItem>>(
                stream,
                _jsonOptions,
                cancellationToken);

            return dados ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP na página {Pagina}: {Url}", pagina, url);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout na página {Pagina}: {Url}", pagina, url);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar página {Pagina}: {Url}", pagina, url);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string endpoint, CancellationToken cancellationToken)
    {
        const int maxTentativas = 5;

        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                return response;

            if (tentativa >= maxTentativas)
                throw new InvalidOperationException("Limite de tentativas excedido (429).");

            var tempoEsperaSegundos = 5;

            try
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var erro = await JsonSerializer.DeserializeAsync<RateLimitResponse>(
                    stream,
                    _jsonOptions,
                    cancellationToken);

                if (Convert.ToInt32(erro?.TempoAteLiberar) > 0)
                    tempoEsperaSegundos = Convert.ToInt32(erro.TempoAteLiberar);
            }
            catch
            {
                _logger.LogWarning("Não foi possível ler tempoAteLiberar do 429. Usando fallback.");
            }

            _logger.LogWarning("429 recebido. Aguardando {Tempo}s…", tempoEsperaSegundos);
            await Task.Delay(TimeSpan.FromSeconds(tempoEsperaSegundos), cancellationToken);
        }

        throw new InvalidOperationException("Unreachable");
    }
}
