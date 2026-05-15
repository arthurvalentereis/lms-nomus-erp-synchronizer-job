namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Sincronização de histórico completo (carga inicial): busca paginada no Nomus e envio em lotes ao Letmesee.
/// </summary>
public interface IFullHistorySynchronizationService
{
    Task ExecuteAsync(
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        string hashToken,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
