using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Services;

namespace lms_nomus_erp_synchronizer_job.Worker.Jobs;

/// <summary>
/// Job de sincronização individual por cliente
/// Responsabilidade: Sincronizar 1 cliente (Boletos, Recebimentos, Contas a Receber)
/// Características:
/// - Retry isolado por cliente
/// - Falha de 1 cliente não afeta outros
/// - Pode ser executado em paralelo por diferentes workers
/// </summary>
public class SyncClienteJob
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<SyncClienteJob> _logger;

    public SyncClienteJob(
        ISynchronizationService synchronizationService,
        ILogger<SyncClienteJob> logger)
    {
        _synchronizationService = synchronizationService;
        _logger = logger;
    }

    /// <summary>
    /// Método executado pelo Hangfire para sincronizar 1 cliente
    /// Usa [DisableConcurrentExecution] para garantir que 1 cliente não seja processado simultaneamente
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 300)] // 5 minutos de timeout por cliente
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })] // Retry com backoff exponencial
    public async Task ExecuteAsync(long userGroupId, string hashToken,string baseUrl, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["JobName"] = nameof(SyncClienteJob),
            ["UserGroupId"] = userGroupId,
            ["BaseUrl"] = baseUrl
        });

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Iniciando sincronização do cliente {UserGroupId}. CorrelationId: {CorrelationId}",
                userGroupId, correlationId);

            await _synchronizationService.SynchronizeClienteAsync(userGroupId, hashToken,baseUrl, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Sincronização do cliente {UserGroupId} concluída com sucesso. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                userGroupId, correlationId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Erro ao sincronizar cliente {UserGroupId}. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                userGroupId, correlationId, duration.TotalMilliseconds);
            throw; // Re-lança para o Hangfire tentar retry
        }
    }
}


