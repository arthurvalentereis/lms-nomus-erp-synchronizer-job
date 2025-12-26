namespace lms_nomus_erp_synchronizer_job.Domain.Models;

/// <summary>
/// Representa um boleto gerado no ERP Nomus
/// </summary>
public class Boleto
{
    public int Id { get; set; }
    public int IdContaReceber { get; set; }
    public int IdPessoa { get; set; }
    public int IdEmpresa { get; set; }
    public DateTime DataHoraEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Cancelado { get; set; }
    public string NossoNumeroBoletoBancario { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NomePessoa { get; set; } = string.Empty;
    public string NomeEmpresa { get; set; } = string.Empty;
}

