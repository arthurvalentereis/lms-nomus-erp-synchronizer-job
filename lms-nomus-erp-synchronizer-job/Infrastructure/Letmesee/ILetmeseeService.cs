using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;

/// <summary>
/// Serviço para integração com a API Letmesee
/// </summary>
public interface ILetmeseeService
{
    /// <summary>
    /// Busca todos os clientes Nomus conectados ao Letmesee com seus tokens
    /// </summary>
    Task<IEnumerable<NomusCustomersDto>> GetNomusCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia uma lista de invoices para o Letmesee
    /// </summary>
    Task SendInvoicesAsync(IEnumerable<RequestInvoiceDto> invoices, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Envia uma lista de invoices para o Letmesee
    /// </summary>
    Task SendCustomerAsync(IEnumerable<RequestCustomerDto> customers, CancellationToken cancellationToken = default);
}

