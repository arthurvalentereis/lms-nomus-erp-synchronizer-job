namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;

/// <summary>
/// Factory para criar instâncias do NomusClient com token específico de cada cliente
/// </summary>
public interface INomusClientFactory
{
    /// <summary>
    /// Cria uma instância do NomusClient configurada com o token do cliente
    /// </summary>
    INomusClient CreateClient(string clientToken,string baseUrl);
}

