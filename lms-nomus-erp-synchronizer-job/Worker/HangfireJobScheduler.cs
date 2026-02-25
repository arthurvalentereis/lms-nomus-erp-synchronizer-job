using Hangfire;
using lms_nomus_erp_synchronizer_job.Worker.Jobs;

namespace lms_nomus_erp_synchronizer_job.Worker;

/// <summary>
/// Configuração e agendamento dos jobs recorrentes do Hangfire
/// </summary>
public static class HangfireJobScheduler
{
    /// <summary>
    /// Agenda o job orquestrador que executa a cada 5 minutos
    /// Este job busca clientes e enfileira jobs individuais (1 por cliente)
    /// </summary>
    public static void ScheduleJobs()
    {
        // Agenda o job orquestrador a cada 5 minutos
        // O Hangfire usa formato cron: */5 * * * * significa "a cada 5 minutos"
        //RecurringJob.AddOrUpdate<ScheduleSyncJob>(
        //    "schedule-sync-orchestrator",
        //    job => job.ExecuteAsync(CancellationToken.None),
        //    "*/5 * * * *", // A cada 1 minutos
        //    new RecurringJobOptions
        //    {
        //        TimeZone = TimeZoneInfo.Local
        //    });
        BackgroundJob.Enqueue<ScheduleSyncJob>(x => x.ExecuteAsync(CancellationToken.None));
        // Nota: 
        // - O job orquestrador usa [DisableConcurrentExecution] para evitar múltiplas execuções
        // - Os jobs individuais (SyncClienteJob) são enfileirados como fire-and-forget
        // - Cada job individual também usa [DisableConcurrentExecution] para evitar processar o mesmo cliente simultaneamente
    }
    /// <summary>
    /// Agenda job orquestrador para userGroupEspecifico 
    /// </summary>
    public static void ScheduleJobsUserGroupId(long userGroupId,long userCompanyId,string creditorDocument,string hashToken, string urlClient)
    {
        BackgroundJob.Enqueue<SyncClienteJob>(x => x.ExecuteUserGroupIdAsync(userGroupId,userCompanyId,creditorDocument,hashToken,urlClient,CancellationToken.None));
    }
}

