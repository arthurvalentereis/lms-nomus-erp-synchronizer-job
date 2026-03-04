using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.DependencyInjection;
using lms_nomus_erp_synchronizer_job.Infrastructure.Extensions;
using lms_nomus_erp_synchronizer_job.Infrastructure.Logging;
using lms_nomus_erp_synchronizer_job.Worker;
using Serilog;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

// =====================
// SERILOG
// =====================
builder.Services.AddSerilog((services, loggerConfig) =>
{
    var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");

    loggerConfig
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
        .WriteTo.BetterStack(
            sourceToken: "m21e1qBWn6nuS3NtRqN49d5C",
            betterStackEndpoint: "https://s1903175.eu-fsn-3.betterstackdata.com"
        );
});


// =====================
// WINDOWS SERVICE
// =====================
var isService = !(Debugger.IsAttached || args.Contains("--console"));

if (isService)
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = ".NET lms-nomus-sync-job";
    });
}

// =====================
// HANGFIRE STORAGE
// =====================
// Configurar Hangfire (requer banco de dados apenas para storage de jobs)
var connectionString = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("Connection string 'HangfireConnection' not found.");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    // WorkerCount pode ser aumentado para processar múltiplos clientes em paralelo
    // Ex: 10 workers = 10 clientes processados simultaneamente
    options.WorkerCount = Environment.ProcessorCount; // Usa número de CPUs disponíveis
});

// =====================
// DI DA APLICAÇÃO
// =====================
// Configurar aplicação e infraestrutura
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Configurar Hangfire Dashboard (opcional - remover em produção se não for necessário)
//app.UseHangfireDashboard("/hangfire", new DashboardOptions
//{
//    Authorization = new[] { new HangfireAuthorizationFilter() } // Permitir acesso local
//});

// =====================
// SCHEDULER SERVICE
// =====================
builder.Services.AddHostedService<HangfireBootstrapper>();

var host = builder.Build();

Log.Information("Aplicação iniciada. Hangfire Dashboard disponível em /hangfire");

try
{
    Log.Information("Iniciando o job...");
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Aplicação iniciada com Serilog!");
    host.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocorreu uma falha crítica ao iniciar o worker.");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Filtro de autorização simples para o Hangfire Dashboard
/// Permite acesso apenas localmente (pode ser melhorado para produção)
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Em produção, implementar autenticação adequada
        var httpContext = context.GetHttpContext();
        return httpContext.Request.IsLocal() || httpContext.User.Identity?.IsAuthenticated == true;
    }
}
