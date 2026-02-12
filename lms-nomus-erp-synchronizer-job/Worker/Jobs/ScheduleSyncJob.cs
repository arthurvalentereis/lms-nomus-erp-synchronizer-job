using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Services;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;

namespace lms_nomus_erp_synchronizer_job.Worker.Jobs;

/// <summary>
/// Job orquestrador (scheduler) que executa a cada 5 minutos
/// Responsabilidade: Buscar clientes e enfileirar 1 job por cliente
/// </summary>
public class ScheduleSyncJob
{
    private readonly ILetmeseeService _letmeseeService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ScheduleSyncJob> _logger;

    public ScheduleSyncJob(
        ILetmeseeService letmeseeService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<ScheduleSyncJob> logger)
    {
        _letmeseeService = letmeseeService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Método executado pelo Hangfire a cada 5 minutos
    /// Busca clientes e enfileira jobs individuais (fire-and-forget)
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 60)] // Evita múltiplas execuções simultâneas
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["JobName"] = nameof(ScheduleSyncJob)
        });

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Iniciando job orquestrador. Buscando clientes para sincronização. CorrelationId: {CorrelationId}", correlationId);

            // Buscar clientes Nomus conectados ao Letmesee
            var customers = await _letmeseeService.GetNomusCustomersAsync(cancellationToken);
            var customersList = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.HashToken))
                .ToList();

            if (!customersList.Any())
            {
                _logger.LogWarning("Nenhum cliente Nomus encontrado. Nenhum job será enfileirado.");
                return;
            }

            _logger.LogInformation("Total de clientes encontrados: {Count}. Enfileirando jobs individuais.", customersList.Count);

            int jobsEnqueued = 0;
            foreach (var customer in customersList)
            {
                try
                {
                    // Enfileirar 1 job por cliente (fire-and-forget)
                    var jobId = _backgroundJobClient.Enqueue<SyncClienteJob>(
                        job => job.ExecuteAsync(customer.UserGroupId!.Value, customer.HashToken!,customer.BaseUrl!, cancellationToken));

                    jobsEnqueued++;
                    _logger.LogDebug("Job enfileirado para cliente {UserGroupId} ({Name}). JobId: {JobId}",
                        customer.UserGroupId, customer.Name, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro ao enfileirar job para cliente {UserGroupId} ({Name}). Continuando com próximo cliente.",
                        customer.UserGroupId, customer.Name);
                    // Continua com próximo cliente mesmo se houver erro
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Job orquestrador concluído. {JobsEnqueued} jobs enfileirados de {TotalCustomers} clientes. Duração: {Duration}ms",
                jobsEnqueued, customersList.Count, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Erro no job orquestrador. CorrelationId: {CorrelationId}, Duração: {Duration}ms",
                correlationId, duration.TotalMilliseconds);
            throw; // Re-lança para o Hangfire registrar a falha
        }
    }
}


