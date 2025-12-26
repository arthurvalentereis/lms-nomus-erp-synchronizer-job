namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Serviço responsável pela sincronização de dados do Nomus
/// </summary>
public interface ISynchronizationService
{
    /// <summary>
    /// Sincroniza todos os boletos do Nomus
    /// </summary>
    Task SynchronizeBoletosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincroniza todos os recebimentos do Nomus
    /// </summary>
    Task SynchronizeRecebimentosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincroniza todas as contas a receber do Nomus
    /// </summary>
    Task SynchronizeContasReceberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincroniza todos os dados (boletos, recebimentos e contas a receber)
    /// </summary>
    Task SynchronizeAllAsync(CancellationToken cancellationToken = default);
}

