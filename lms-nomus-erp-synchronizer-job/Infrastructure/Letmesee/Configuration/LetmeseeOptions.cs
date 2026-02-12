namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.Configuration;

/// <summary>
/// Configurações do cliente Letmesee
/// </summary>
public class LetmeseeOptions
{
    public const string SectionName = "Letmesee";

    /// <summary>
    /// URL base da API do Letmesee
    /// </summary>
    public string BaseUrl { get; set; } = "https://production.letmesee.com.br/";

    /// <summary>
    /// Token de autenticação da API Letmesee
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

