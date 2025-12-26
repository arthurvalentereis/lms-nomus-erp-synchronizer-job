namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Configuration;

/// <summary>
/// Configurações do cliente Nomus
/// </summary>
public class NomusOptions
{
    public const string SectionName = "Nomus";

    /// <summary>
    /// URL base da API do Nomus (ex: https://empresa.nomus.com.br)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Token de autenticação (Basic auth)
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Timeout em segundos para requisições HTTP (padrão: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número de tentativas de retry em caso de falha (padrão: 3)
    /// </summary>
    public int RetryCount { get; set; } = 3;
}

