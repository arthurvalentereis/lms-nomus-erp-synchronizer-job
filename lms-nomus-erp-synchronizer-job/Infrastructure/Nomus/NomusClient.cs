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
    public async Task<IEnumerable<CustomerDto>> GetCustomerAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<CustomerDto>(url, cancellationToken);
    public async Task<IEnumerable<BoletoDto>> GetBoletosAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<BoletoDto>(url, cancellationToken);

    public async Task<IEnumerable<RecebimentoDto>> GetRecebimentosAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<RecebimentoDto>(url, cancellationToken);

    public async Task<IEnumerable<ContaReceberDto>> GetContasReceberAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<ContaReceberDto>(url, cancellationToken);
    public async Task<IEnumerable<CustomerDto>> GetAllCustomerAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<CustomerDto>(url, cancellationToken);
    public async Task<IEnumerable<BoletoDto>> GetAllBoletosAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<BoletoDto>(url, cancellationToken);

    public async Task<IEnumerable<RecebimentoDto>> GetAllRecebimentosAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<RecebimentoDto>(url, cancellationToken);

    public async Task<IEnumerable<ContaReceberDto>> GetAllContasReceberAsync(string url, CancellationToken cancellationToken = default)
    => await GetAsync<ContaReceberDto>(url, cancellationToken);

    private const int MaxPaginas = 100;
    private const string ParametroPagina = "pagina"; // conforme documentação Nomus (Postman)

    private async Task<List<TItem>> GetAsync<TItem>(string endpointBase, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fazendo requisição GET paginada para: {Endpoint} (máx. {Max} páginas)", endpointBase, MaxPaginas);

            var resultados = new List<TItem>();
            var pagina = 1;
            var separator = endpointBase.Contains('?') ? "&" : "?";

            while (pagina <= MaxPaginas)
            {
                var endpoint = $"{endpointBase}{separator}{ParametroPagina}={pagina}";

                _logger.LogDebug("Buscando página {Pagina}: {Endpoint}", pagina, endpoint);

                var response = await SendWithRetryAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var dados = await JsonSerializer.DeserializeAsync<List<TItem>>(
                    stream,
                    _jsonOptions,
                    cancellationToken);

                if (dados == null || dados.Count == 0)
                {
                    _logger.LogDebug("Página {Pagina} vazia ou nula, encerrando paginação.", pagina);
                    break;
                }

                resultados.AddRange(dados);
                _logger.LogDebug("Página {Pagina}: {Count} itens (total acumulado: {Total}).", pagina, dados.Count, resultados.Count);
                pagina++;
            }

            if (pagina > MaxPaginas)
                _logger.LogWarning("Limite de {Max} páginas atingido para: {Endpoint}. Total de itens: {Total}.", MaxPaginas, endpointBase, resultados.Count);

            _logger.LogDebug("Requisição paginada concluída para: {Endpoint}. Páginas: {Paginas}, Total itens: {Total}.", endpointBase, pagina - 1, resultados.Count);
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

