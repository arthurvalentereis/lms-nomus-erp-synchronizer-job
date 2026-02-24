using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using lms_nomus_erp_synchronizer_job.Domain.Models;
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
    public async Task<IEnumerable<CustomerDto>> GetCustomerAsync(CancellationToken cancellationToken = default)
    => await GetAsync<CustomerDto>("rest/clientes", cancellationToken);
    public async Task<IEnumerable<BoletoDto>> GetBoletosAsync(CancellationToken cancellationToken = default)
    => await GetAsync<BoletoDto>("rest/boletos", cancellationToken);

    public async Task<IEnumerable<RecebimentoDto>> GetRecebimentosAsync(CancellationToken cancellationToken = default)
    => await GetAsync<RecebimentoDto>("rest/recebimentos", cancellationToken);

    public async Task<IEnumerable<ContaReceberDto>> GetContasReceberAsync(CancellationToken cancellationToken = default)
    => await GetAsync<ContaReceberDto>("rest/contasReceber", cancellationToken);

    private async Task<List<TItem>> GetAsync<TItem>(string endpointBase, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fazendo requisição GET para: {Endpoint}", endpointBase);

            var resultados = new List<TItem>();
            var pagina = 1;

            while (pagina <= 100)
            {
                var endpoint = $"{endpointBase}?page={pagina}";

                var response = await SendWithRetryAsync(endpoint, cancellationToken);

                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                var dados = await JsonSerializer.DeserializeAsync<List<TItem>>(
                    stream,
                    _jsonOptions,
                    cancellationToken);

                if (dados == null || dados.Count == 0)
                    break;

                resultados.AddRange(dados);

                pagina++;
            }

            _logger.LogDebug("Requisição paginada concluída para: {Endpoint}.", endpointBase);
            return resultados;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao buscar dados do endpoint: {Endpoint}", endpointBase);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout ao buscar dados do endpoint: {Endpoint}", endpointBase);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta do endpoint: {Endpoint}", endpointBase);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar dados do endpoint: {Endpoint}", endpointBase);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
    string endpoint,
    CancellationToken cancellationToken)
    {
        const int maxTentativas = 5;
        int tentativa = 0;

        while (true)
        {
            tentativa++;

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                return response;

            if (tentativa >= maxTentativas)
                throw new Exception("Limite de tentativas excedido (429).");

            int tempoEsperaSegundos = 5; // fallback padrão

            try
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                var erro = await JsonSerializer.DeserializeAsync<RateLimitResponse>(
                    stream,
                    _jsonOptions,
                    cancellationToken);

                if (Convert.ToInt32(erro?.TempoAteLiberar) > 0)
                    tempoEsperaSegundos = Convert.ToInt32(erro?.TempoAteLiberar);
            }
            catch
            {
                _logger.LogWarning("Não foi possível ler tempoAteLiberar do 429. Usando fallback.");
            }

            _logger.LogWarning(
                "429 recebido. Aguardando {Tempo}s antes de tentar novamente...",
                tempoEsperaSegundos);

            await Task.Delay(TimeSpan.FromSeconds(tempoEsperaSegundos), cancellationToken);
        }
    }
}

