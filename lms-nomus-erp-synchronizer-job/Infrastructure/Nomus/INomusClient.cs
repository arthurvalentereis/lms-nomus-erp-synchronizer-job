using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Cliente para integração com a API REST do ERP Nomus
/// </summary>
public interface INomusClient
{
    /// <summary>
    /// Busca todos os boletos gerados
    /// </summary>
    Task<IEnumerable<CustomerDto>> GetCustomerAsync(string url , CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todos os boletos gerados
    /// </summary>
    Task<IEnumerable<BoletoDto>> GetBoletosAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todos os recebimentos
    /// </summary>
    Task<IEnumerable<RecebimentoDto>> GetRecebimentosAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todas as contas a receber
    /// </summary>
    Task<IEnumerable<ContaReceberDto>> GetContasReceberAsync(string url, CancellationToken cancellationToken = default);
}

