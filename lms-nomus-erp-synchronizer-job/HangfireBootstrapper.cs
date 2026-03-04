using Hangfire;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class HangfireBootstrapper : IHostedService
{
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfireBootstrapper> _logger;

    public HangfireBootstrapper(
        IServiceProvider provider,
        IConfiguration configuration,
        ILogger<HangfireBootstrapper> logger)
    {
        _provider = provider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando agendamento de jobs...");

        using var scope = _provider.CreateScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<HangfireJobScheduler>();

        var userGroupConfiguration =
            _configuration.GetSection("UserGroupConfiguration")
                          .Get<UserGroupConfiguration>();

        if (userGroupConfiguration is not null)
        {
            if (!userGroupConfiguration.Run)
            {
                scheduler.ScheduleJobs();
            }
            else
            {
                scheduler.ScheduleJobsUserGroupId(
                    userGroupConfiguration.UserGroupId,
                    userGroupConfiguration.UserCompanyId,
                    userGroupConfiguration.CreditorDocument,
                    userGroupConfiguration.TokenUser,
                    userGroupConfiguration.UrlUser);
            }
        }

        _logger.LogInformation("Jobs agendados com sucesso.");

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker finalizando...");
        return Task.CompletedTask;
    }
}