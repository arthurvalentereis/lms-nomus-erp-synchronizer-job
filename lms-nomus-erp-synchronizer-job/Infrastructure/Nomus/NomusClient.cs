using System.Net.Http.Headers;
using System.Text.Json;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Implementação do cliente HTTP para integração com a API REST do ERP Nomus
/// Utiliza HttpClientFactory, Polly para retry policies e tratamento resiliente de erros
/// </summary>
public class NomusClient : INomusClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NomusClient> _logger;
    private readonly NomusOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

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

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // HttpClient já deve estar configurado pela factory ou construtor
        // Apenas garante que o Accept header está configurado se não estiver
        if (!_httpClient.DefaultRequestHeaders.Accept.Contains(
            new MediaTypeWithQualityHeaderValue("application/json")))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    /// <summary>
    /// Cria uma política de retry para requisições HTTP
    /// Retry em caso de falhas transitórias (5xx, 408, network errors)
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log será feito pelo middleware/logging automático
                });
    }

    public async Task<IEnumerable<BoletoDto>> GetBoletosAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/empresa/rest/boletos";
        return await GetAsync<IEnumerable<BoletoDto>>(endpoint, cancellationToken);
    }

    public async Task<IEnumerable<RecebimentoDto>> GetRecebimentosAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/empresa/rest/recebimentos";
        return await GetAsync<IEnumerable<RecebimentoDto>>(endpoint, cancellationToken);
    }

    public async Task<IEnumerable<ContaReceberDto>> GetContasReceberAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/empresa/rest/contasReceber";
        return await GetAsync<IEnumerable<ContaReceberDto>>(endpoint, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fazendo requisição GET para: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            
            if (result == null)
            {
                _logger.LogWarning("Resposta vazia do endpoint: {Endpoint}", endpoint);
                return default!;
            }

            _logger.LogDebug("Requisição bem-sucedida para: {Endpoint}. Tamanho da resposta: {Size} bytes", 
                endpoint, content.Length);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao buscar dados do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout ao buscar dados do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar dados do endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
}

