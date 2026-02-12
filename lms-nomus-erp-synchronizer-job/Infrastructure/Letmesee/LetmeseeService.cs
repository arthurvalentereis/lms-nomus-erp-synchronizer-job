using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.Configuration;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using Microsoft.Extensions.Options;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;

/// <summary>
/// Implementação do serviço para integração com a API Letmesee
/// </summary>
public class LetmeseeService : ILetmeseeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LetmeseeService> _logger;
    private readonly LetmeseeOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public LetmeseeService(
        HttpClient httpClient,
        IOptions<LetmeseeOptions> options,
        ILogger<LetmeseeService> logger)
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
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        if (!string.IsNullOrEmpty(_options.AuthToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.AuthToken);
        }
        
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IEnumerable<NomusCustomersDto>> GetNomusCustomersAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "erp/nomus/listcustomers";
        
        try
        {
            _logger.LogDebug("Buscando clientes Nomus do Letmesee. Endpoint: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var customers = JsonSerializer.Deserialize<IEnumerable<NomusCustomersDto>>(content, _jsonOptions);

            if (customers == null)
            {
                _logger.LogWarning("Resposta vazia do endpoint: {Endpoint}", endpoint);
                return Enumerable.Empty<NomusCustomersDto>();
            }

            _logger.LogInformation("Total de clientes Nomus encontrados: {Count}", customers.Count());
            return customers;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao buscar clientes Nomus do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout ao buscar clientes Nomus do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta do endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar clientes Nomus do endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task SendInvoicesAsync(IEnumerable<RequestInvoiceDto> invoices, CancellationToken cancellationToken = default)
    {
        const string endpoint = "CustomerInvoice/add-list";
        var invoicesList = invoices.ToList();

        if (!invoicesList.Any())
        {
            _logger.LogWarning("Nenhuma invoice para enviar");
            return;
        }

        try
        {
            _logger.LogInformation("Enviando {Count} invoices para o Letmesee", invoicesList.Count);

            var json = JsonSerializer.Serialize(invoicesList, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Invoices enviadas com sucesso. Resposta: {Response}", responseContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao enviar invoices para o endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout ao enviar invoices para o endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar invoices para o endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
}

