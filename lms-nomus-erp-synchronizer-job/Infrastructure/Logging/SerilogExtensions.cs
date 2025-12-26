using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Logging;

/// <summary>
/// Extensões para configuração do Serilog
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configura o Serilog com logs estruturados em JSON
    /// </summary>
    public static void ConfigureSerilog(this IHostBuilder builder)
    {
        builder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    path: "logs/lms-nomus-sync-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning);
        });
    }
}

