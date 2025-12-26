using lms_nomus_erp_synchronizer_job.Application.Services;
using Hangfire;

namespace lms_nomus_erp_synchronizer_job.Jobs;

/// <summary>
/// Job recorrente do Hangfire para sincronização automática de dados do Nomus
/// Executa a cada 5 minutos
/// </summary>
public class NomusSynchronizationJob
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<NomusSynchronizationJob> _logger;

    public NomusSynchronizationJob(
        ISynchronizationService synchronizationService,
        ILogger<NomusSynchronizationJob> logger)
    {
        _synchronizationService = synchronizationService;
        _logger = logger;
    }

    /// <summary>
    /// Método executado pelo Hangfire
    /// Usa [DisableConcurrentExecution] para evitar execuções concorrentes
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 600)] // 10 minutos de timeout
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["JobName"] = nameof(NomusSynchronizationJob)
        });

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Iniciando execução do job de sincronização do Nomus. CorrelationId: {CorrelationId}", correlationId);

            await _synchronizationService.SynchronizeAllAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Job de sincronização do Nomus concluído com sucesso. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                correlationId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Erro ao executar job de sincronização do Nomus. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                correlationId, duration.TotalMilliseconds);
            throw; // Re-lança para o Hangfire registrar a falha
        }
    }
}

