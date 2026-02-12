using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

/// <summary>
/// DTO para parcelas de invoice
/// </summary>
public class InvoiceInstalltmentDto
{
    [JsonPropertyName("invoiceValue")]
    public decimal? InvoiceValue { get; set; }

    [JsonPropertyName("invoiceDueDate")]
    public DateTime? InvoiceDueDate { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }
}

