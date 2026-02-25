using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using lms_nomus_erp_synchronizer_job.Infrastructure.DependencyInjection;
using lms_nomus_erp_synchronizer_job.Infrastructure.Logging;
using lms_nomus_erp_synchronizer_job.Infrastructure.Extensions;
using Serilog;
using lms_nomus_erp_synchronizer_job.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog primeiro
builder.Host.ConfigureSerilog();

// Configurar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

// Configurar aplicação e infraestrutura
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configurar Hangfire Dashboard (opcional - remover em produção se não for necessário)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() } // Permitir acesso local
});

// Agendar jobs recorrentes
lms_nomus_erp_synchronizer_job.Worker.HangfireJobScheduler.ScheduleJobs();
//novo pifio
var userGroupConfiguration = builder.Configuration
    .GetSection("UserGroupConfiguration")
    .Get<UserGroupConfiguration>();

//if(userGroupConfiguration != null)
// lms_nomus_erp_synchronizer_job.Worker.HangfireJobScheduler.ScheduleJobsUserGroupId(userGroupConfiguration.UserGroupId, userGroupConfiguration.UserCompanyId, userGroupConfiguration.CreditorDocument, userGroupConfiguration.TokenUser, userGroupConfiguration.UrlUser);

app.MapControllers();

Log.Information("Aplicação iniciada. Hangfire Dashboard disponível em /hangfire");

app.Run();

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
