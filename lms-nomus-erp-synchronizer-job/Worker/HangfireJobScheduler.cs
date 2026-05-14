using Hangfire;
using lms_nomus_erp_synchronizer_job.Worker.Jobs;

namespace lms_nomus_erp_synchronizer_job.Worker;

/// <summary>
/// Configuração e agendamento dos jobs do Hangfire.
/// O próximo <see cref="ScheduleSyncJob"/> após a primeira subida vem de <c>IOrchestratorNextRunScheduler</c>
/// (após cada sync de cliente ou lista vazia / erro no orquestrador).
/// </summary>
public class HangfireJobScheduler
{
    /// <summary>
    /// Id do RecurringJob usado em versão anterior; pode ainda existir no SQL do Hangfire e disparar o orquestrador fora do intervalo desejado.
    /// </summary>
    public const string LegacyOrchestratorRecurringJobId = "nomus-schedule-sync-orchestrator";

    private readonly IBackgroundJobClient _jobs;

    public HangfireJobScheduler(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    /// <summary>
    /// Remove recurring legado (se existir), enfileira a primeira execução do orquestrador.
    /// </summary>
    public void ScheduleJobs()
    {
        RecurringJob.RemoveIfExists(LegacyOrchestratorRecurringJobId);
        _jobs.Enqueue<ScheduleSyncJob>(x => x.ExecuteAsync());
    }
    /// <summary>
    /// Agenda job orquestrador para userGroupEspecifico 
    /// </summary>
    public void ScheduleJobsUserGroupId(long userGroupId,long userCompanyId,string creditorDocument,string hashToken, string urlClient)
    {
        _jobs.Enqueue<SyncClienteJob>(x => x.ExecuteUserGroupIdAsync(userGroupId, userCompanyId, creditorDocument, hashToken, urlClient));
    }
}

