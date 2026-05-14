namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Agenda a próxima execução do job orquestrador no Hangfire.
/// Substitui um agendamento pendente anterior (debounce) para não acumular N jobs quando há vários clientes.
/// </summary>
public interface IOrchestratorNextRunScheduler
{
    void ScheduleNext(TimeSpan delay);
}
