using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

/// <summary>
/// DTO para requisição de cadastro de invoice no Letmesee
/// </summary>
public class RequestInvoiceDto
{
    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("userGroupId")]
    public long? UserGroupId { get; set; }

    [JsonPropertyName("userCpf")]
    public string? UserCpf { get; set; }

    [JsonPropertyName("userCnpj")]
    public string? UserCnpj { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("invoiceValue")]
    public decimal? InvoiceValue { get; set; }

    [JsonPropertyName("invoiceDueDate")]
    public DateTime? InvoiceDueDate { get; set; }

    [JsonPropertyName("issueDate")]
    public DateTime? IssueDate { get; set; }

    [JsonPropertyName("bankSlipUrl")]
    public string? BankSlipUrl { get; set; }

    [JsonPropertyName("customerId")]
    public long? CustomerId { get; set; }

    [JsonPropertyName("creditorDocument")]
    public string CreditorDocument { get; set; } = string.Empty;

    [JsonPropertyName("buyerId")]
    public string? BuyerId { get; set; }

    [JsonPropertyName("buyerDocument")]
    public string? BuyerDocument { get; set; }

    [JsonPropertyName("buyerName")]
    public string? BuyerName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("penalty")]
    public decimal? Penalty { get; set; }

    [JsonPropertyName("protest")]
    public decimal? Protest { get; set; }

    [JsonPropertyName("courtCost")]
    public decimal? CourtCost { get; set; }

    [JsonPropertyName("discount")]
    public decimal? Discount { get; set; }

    [JsonPropertyName("fees")]
    public decimal? Fees { get; set; }

    [JsonPropertyName("cofins")]
    public decimal? Cofins { get; set; }

    [JsonPropertyName("csll")]
    public decimal? Csll { get; set; }

    [JsonPropertyName("inss")]
    public decimal? Inss { get; set; }

    [JsonPropertyName("iss")]
    public decimal? Iss { get; set; }

    [JsonPropertyName("irpj")]
    public decimal? Irpj { get; set; }

    [JsonPropertyName("others")]
    public decimal? Others { get; set; }

    [JsonPropertyName("pis")]
    public decimal? Pis { get; set; }

    [JsonPropertyName("filePathInvoice")]
    public string? FilePathInvoice { get; set; }

    [JsonPropertyName("filePathTicket")]
    public string? FilePathTicket { get; set; }

    [JsonPropertyName("discountPercentage")]
    public decimal? DiscountPercentage { get; set; }

    [JsonPropertyName("invoiceInstallment")]
    public int? InvoiceInstallment { get; set; }

    [JsonPropertyName("managementOfDefaulters")]
    public bool? ManagementOfDefaulters { get; set; }

    [JsonPropertyName("sentToManagementOfDefaulters")]
    public DateTime? SentToManagementOfDefaulters { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("ourNumber")]
    public string? OurNumber { get; set; }

    [JsonPropertyName("bankId")]
    public long? BankId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("productId")]
    public long? ProductId { get; set; }

    [JsonPropertyName("inExtract")]
    public bool? InExtract { get; set; }

    [JsonPropertyName("installments")]
    public List<InvoiceInstalltmentDto>? Installments { get; set; } = new();

    [JsonPropertyName("repeatEvery")]
    public long? RepeatEvery { get; set; }

    [JsonPropertyName("repeatEveryType")]
    public string? RepeatEveryType { get; set; }

    [JsonPropertyName("repeatEveryEnd")]
    public string? RepeatEveryEnd { get; set; }

    [JsonPropertyName("repeatEveryEndDate")]
    public DateTime? RepeatEveryEndDate { get; set; }
}

