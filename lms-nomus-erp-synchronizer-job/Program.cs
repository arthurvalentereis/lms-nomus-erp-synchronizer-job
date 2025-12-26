using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using lms_nomus_erp_synchronizer_job.Infrastructure.DependencyInjection;
using lms_nomus_erp_synchronizer_job.Infrastructure.Extensions;
using lms_nomus_erp_synchronizer_job.Infrastructure.Logging;
using lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog primeiro
builder.Host.ConfigureSerilog();

// Configurar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configurar Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configurar Hangfire
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
    options.WorkerCount = 1; // Pode ser ajustado conforme necessário
});

// Configurar aplicação e infraestrutura
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Garantir que o banco de dados está criado
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configurar Hangfire Dashboard (opcional - remover em produção se não for necessário)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() } // Permitir acesso local
});

// Agendar jobs recorrentes
lms_nomus_erp_synchronizer_job.Workers.HangfireJobScheduler.ScheduleJobs();

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
