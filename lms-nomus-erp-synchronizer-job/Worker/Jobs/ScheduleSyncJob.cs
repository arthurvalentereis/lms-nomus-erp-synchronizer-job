using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Services;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;

namespace lms_nomus_erp_synchronizer_job.Worker.Jobs;

/// <summary>
/// Job orquestrador: busca clientes e enfileira um job por cliente.
/// O próximo ciclo global é agendado ao terminar cada sync de cliente (sucesso 5 min / erro 15 min)
/// ou aqui quando não há clientes (5 min) / falha ao buscar lista (15 min).
/// </summary>
public class ScheduleSyncJob
{
    private const int MinutosAteProximoCicloSucesso = 5;
    private const int MinutosAteProximoCicloErro = 15;

    private readonly ILetmeseeService _letmeseeService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IOrchestratorNextRunScheduler _orchestratorNextRun;
    private readonly ILogger<ScheduleSyncJob> _logger;

    public ScheduleSyncJob(
        ILetmeseeService letmeseeService,
        IBackgroundJobClient backgroundJobClient,
        IOrchestratorNextRunScheduler orchestratorNextRun,
        ILogger<ScheduleSyncJob> logger)
    {
        _letmeseeService = letmeseeService;
        _backgroundJobClient = backgroundJobClient;
        _orchestratorNextRun = orchestratorNextRun;
        _logger = logger;
    }

    /// <summary>
    /// Orquestrador: busca clientes e enfileira um job por cliente.
    /// Sem <c>CancellationToken</c> nos argumentos Hangfire (evita Arguments vazios na tabela Job).
    /// <c>AutomaticRetry(Attempts = 0)</c>: em caso de exceção, o próximo ciclo é controlado APENAS pelo
    /// <c>ScheduleNext(15 min)</c> chamado no <c>catch</c>. Sem o atributo, o Hangfire reagendaria o
    /// mesmo job em ~30 s pelo retry default global, dando a impressão de "rodar imediatamente".
    /// </summary>
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExecuteAsync()
    {
        var cancellationToken = CancellationToken.None;

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
                _orchestratorNextRun.ScheduleNext(TimeSpan.FromMinutes(MinutosAteProximoCicloSucesso));
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
                        job => job.ExecuteAsync(
                            customer.UserGroupId!.Value,
                            customer.UserCompanyId!.Value,
                            customer.CreditorDocument!,
                            customer.HashToken!,
                            customer.BaseUrl!));

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
            _orchestratorNextRun.ScheduleNext(TimeSpan.FromMinutes(MinutosAteProximoCicloErro));
            throw; // Re-lança para o Hangfire registrar a falha
        }
    }
}
