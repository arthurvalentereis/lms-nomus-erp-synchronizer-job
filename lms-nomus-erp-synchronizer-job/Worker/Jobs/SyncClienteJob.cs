using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Services;

namespace lms_nomus_erp_synchronizer_job.Worker.Jobs;

/// <summary>
/// Job de sincronização individual por cliente
/// Responsabilidade: Sincronizar 1 cliente
/// - Contas a receber / recebimentos / customers
/// - Contas a pagar / pagamentos / suppliers
/// Características:
/// - Retry isolado por cliente
/// - Falha de 1 cliente não afeta outros
/// - Pode ser executado em paralelo por diferentes workers
/// </summary>
public class SyncClienteJob
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly IPayableSynchronizationService _payableSynchronizationService;
    private readonly ILogger<SyncClienteJob> _logger;

    public SyncClienteJob(
        ISynchronizationService synchronizationService,
        IPayableSynchronizationService payableSynchronizationService,
        ILogger<SyncClienteJob> logger)
    {
        _synchronizationService = synchronizationService;
        _payableSynchronizationService = payableSynchronizationService;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza um cliente (AR + AP D-1). Sem <c>CancellationToken</c> nos argumentos Hangfire (serialização).
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 300)] // 5 minutos de timeout por cliente
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })] // Retry com backoff exponencial
    public async Task ExecuteAsync(long userGroupId, long userCompanyId, string creditorDocument, string hashToken, string baseUrl)
    {
        var cancellationToken = CancellationToken.None;

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

            await _synchronizationService.SynchronizeClienteAsync(userGroupId, userCompanyId, creditorDocument, hashToken, baseUrl, cancellationToken);
            await _payableSynchronizationService.SynchronizeClienteAsync(userGroupId, userCompanyId, creditorDocument, hashToken, baseUrl, cancellationToken);

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

    /// <summary>
    /// Histórico completo (AR + AP). Usa [DisableConcurrentExecution] para garantir que 1 cliente não seja processado simultaneamente.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 300)] // 5 minutos de timeout por cliente
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })] // Retry com backoff exponencial
    public async Task ExecuteUserGroupIdAsync(long userGroupId, long userCompanyId, string creditorDocument, string hashToken, string baseUrl)
    {
        var cancellationToken = CancellationToken.None;

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
                "Iniciando sincronização de histórico do cliente {UserGroupId}. CorrelationId: {CorrelationId}",
                userGroupId, correlationId);

            await _synchronizationService.SynchronizeAllFilesClienteAsync(userGroupId, userCompanyId, creditorDocument, hashToken, baseUrl, cancellationToken);
            await _payableSynchronizationService.SynchronizeAllFilesClienteAsync(userGroupId, userCompanyId, creditorDocument, hashToken, baseUrl, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Sincronização de histórico do cliente {UserGroupId} concluída com sucesso. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                userGroupId, correlationId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Erro ao sincronizar histórico do cliente {UserGroupId}. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                userGroupId, correlationId, duration.TotalMilliseconds);
            throw; // Re-lança para o Hangfire tentar retry
        }
    }
}


