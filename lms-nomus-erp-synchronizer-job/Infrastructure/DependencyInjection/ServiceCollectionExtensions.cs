using lms_nomus_erp_synchronizer_job.Application.Services;
using lms_nomus_erp_synchronizer_job.Worker;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.Configuration;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Extensions.Http;
using System.Net;

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

        // HttpClient nomeado: BaseAddress e token vêm do NomusClientFactory por cliente
        services.AddHttpClient("Nomus")
            .AddResilienceHandler("nomus-pipeline", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = nomusOptions?.RetryCount ?? 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });
            });

        // Factory para criar clientes Nomus com tokens específicos
        services.AddSingleton<INomusClientFactory, NomusClientFactory>();

        // Configuração do Letmesee
        services.Configure<LetmeseeOptions>(
            configuration.GetSection(LetmeseeOptions.SectionName));

        var letmeseeOptions = configuration.GetSection(LetmeseeOptions.SectionName).Get<LetmeseeOptions>();

        // HttpClient para Letmesee com Polly
        services.AddHttpClient<ILetmeseeService, LetmeseeService>(client =>
        {
            client.BaseAddress = new Uri(letmeseeOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(letmeseeOptions.TimeoutSeconds);
        })
       .AddResilienceHandler("letmesee-pipeline", builder =>
       {
           builder
               .AddRetry(new HttpRetryStrategyOptions
               {
                   MaxRetryAttempts = letmeseeOptions?.RetryCount ?? 3,
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
        return services;
    }

    /// <summary>
    /// Adiciona os serviços da camada de Application
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrchestratorNextRunScheduler, OrchestratorNextRunScheduler>();
        services.AddScoped<IFullHistorySynchronizationService, FullHistorySynchronizationService>();
        services.AddScoped<ISynchronizationService, SynchronizationService>();
        services.AddTransient<HangfireJobScheduler>();
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

