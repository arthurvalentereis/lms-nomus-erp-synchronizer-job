namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Sincronização de contas a pagar / suppliers do Nomus via Letmesee
/// </summary>
public interface IPayableSynchronizationService
{
    Task SynchronizeClienteAsync(
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        string hashToken,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task SynchronizeAllFilesClienteAsync(
        long userGroupId,
        long userCompanyId,
        string creditorDocument,
        string hashToken,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
