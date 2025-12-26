using Hangfire;

namespace lms_nomus_erp_synchronizer_job.Workers;

/// <summary>
/// Configuração e agendamento dos jobs recorrentes do Hangfire
/// </summary>
public static class HangfireJobScheduler
{
    /// <summary>
    /// Agenda o job de sincronização do Nomus para executar a cada 5 minutos
    /// </summary>
    public static void ScheduleJobs()
    {
        // Agenda o job de sincronização a cada 5 minutos
        // O Hangfire usa formato cron: */5 * * * * significa "a cada 5 minutos"
        RecurringJob.AddOrUpdate<Jobs.NomusSynchronizationJob>(
            "nomus-synchronization",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *", // A cada 5 minutos
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        // Nota: O job é reentrante, mas usamos [DisableConcurrentExecution] no método
        // para garantir que não haja execuções concorrentes
    }
}

