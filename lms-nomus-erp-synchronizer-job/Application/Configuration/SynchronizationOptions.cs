namespace lms_nomus_erp_synchronizer_job.Application.Configuration;

/// <summary>
/// Configurações para o serviço de sincronização
/// </summary>
public class SynchronizationOptions
{
    public const string SectionName = "Synchronization";

    /// <summary>
    /// Número máximo de clientes processados em paralelo (padrão: 10)
    /// </summary>
    public int MaxConcurrentCustomers { get; set; } = 10;

    /// <summary>
    /// Tamanho do lote (batch) para envio de invoices ao Letmesee (padrão: 100)
    /// </summary>
    public int InvoiceBatchSize { get; set; } = 100;

    /// <summary>
    /// Tamanho do lote (batch) para operações de persistência no banco (padrão: 500)
    /// </summary>
    public int PersistenceBatchSize { get; set; } = 500;

    /// <summary>
    /// Timeout em segundos para processamento de cada cliente (padrão: 60)
    /// </summary>
    public int CustomerProcessingTimeoutSeconds { get; set; } = 60;
}

