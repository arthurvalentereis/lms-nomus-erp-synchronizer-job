using System.Net.Http.Headers;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;
using Microsoft.Extensions.Options;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Factory para criar instâncias do NomusClient configuradas com token específico
/// </summary>
public class NomusClientFactory : INomusClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NomusOptions _options;
    private readonly ILogger<NomusClient> _logger;

    public NomusClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<NomusOptions> options,
        ILogger<NomusClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public INomusClient CreateClient(string clientToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Configura o HttpClient com o token específico do cliente
        httpClient.BaseAddress = new Uri(_options.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", clientToken);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return new NomusClient(httpClient, Microsoft.Extensions.Options.Options.Create(_options), _logger);
    }
}

