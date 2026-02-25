namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Serviço responsável pela sincronização de dados do Nomus via Letmesee
/// </summary>
public interface ISynchronizationService
{
    /// <summary>
    /// Sincroniza dados de um único cliente
    /// </summary>
    /// <param name="userGroupId">ID do grupo de usuários (cliente)</param>
    /// <param name="hashToken">Token de autenticação do cliente no Nomus</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task SynchronizeClienteAsync(long userGroupId,long userCompanyId,string creditorDocument, string hashToken,string baseUrl, CancellationToken cancellationToken = default);
}
