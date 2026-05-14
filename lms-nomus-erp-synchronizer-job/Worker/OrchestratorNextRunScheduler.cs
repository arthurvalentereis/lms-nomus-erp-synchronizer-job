using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Services;
using lms_nomus_erp_synchronizer_job.Worker.Jobs;

namespace lms_nomus_erp_synchronizer_job.Worker;

/// <summary>
/// Garante um único ScheduleSyncJob agendado: substitui o anterior se ainda estiver pendente.
/// </summary>
public sealed class OrchestratorNextRunScheduler : IOrchestratorNextRunScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<OrchestratorNextRunScheduler> _logger;
    private readonly object _gate = new();
    private string? _pendingJobId;

    public OrchestratorNextRunScheduler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<OrchestratorNextRunScheduler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public void ScheduleNext(TimeSpan delay)
    {
        lock (_gate)
        {
            if (!string.IsNullOrEmpty(_pendingJobId))
            {
                try
                {
                    _backgroundJobClient.Delete(_pendingJobId);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Não foi possível remover agendamento anterior {JobId}", _pendingJobId);
                }
            }

            var enqueueAt = DateTimeOffset.UtcNow.Add(delay);
            _pendingJobId = _backgroundJobClient.Schedule<ScheduleSyncJob>(
                job => job.ExecuteAsync(),
                enqueueAt);

            _logger.LogInformation(
                "Próximo orquestrador agendado para {EnqueueAtUtc:o} (UTC), daqui a {Delay}. JobId: {JobId}",
                enqueueAt.UtcDateTime,
                delay,
                _pendingJobId);
        }
    }
}
