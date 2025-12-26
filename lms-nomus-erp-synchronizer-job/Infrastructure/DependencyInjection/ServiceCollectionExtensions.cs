using System.Net;
using lms_nomus_erp_synchronizer_job.Application.Services;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;
using lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Extensions.Http;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.DependencyInjection;

/// <summary>
/// Extensões para configuração de injeção de dependências
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona os serviços da camada de Infrastructure
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração do Nomus
        services.Configure<NomusOptions>(
            configuration.GetSection(NomusOptions.SectionName));

        var nomusOptions = configuration.GetSection(NomusOptions.SectionName).Get<NomusOptions>();

        // HttpClient para Nomus com Polly
        services.AddHttpClient<INomusClient, NomusClient>(client =>
        {
            client.BaseAddress = new Uri(nomusOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(nomusOptions.TimeoutSeconds);
        })
        .AddResilienceHandler("nomus-pipeline", builder =>
        {
            builder
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = nomusOptions?.RetryCount ?? 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30)
                });
        });
       

        // Entity Framework
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repositórios
        services.AddScoped<IRepository<Boleto>, Repository<Boleto>>();
        services.AddScoped<IRepository<Recebimento>, Repository<Recebimento>>();
        services.AddScoped<IRepository<ContaReceber>, Repository<ContaReceber>>();

        return services;
    }

    /// <summary>
    /// Adiciona os serviços da camada de Application
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISynchronizationService, SynchronizationService>();
        return services;
    }

    /// <summary>
    /// Configura política de retry para requisições HTTP
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Configura política de circuit breaker para evitar chamadas excessivas em caso de falhas
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}

