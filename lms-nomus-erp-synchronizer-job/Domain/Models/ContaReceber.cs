namespace lms_nomus_erp_synchronizer_job.Domain.Models;

/// <summary>
/// Representa uma conta a receber no ERP Nomus
/// </summary>
public class ContaReceber
{
    public int Id { get; set; }
    public int IdPessoa { get; set; }
    public int IdEmpresa { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime DataCompetencia { get; set; }
    public decimal ValorReceber { get; set; }
    public decimal ValorRecebido { get; set; }
    public decimal SaldoReceber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public string NomePessoa { get; set; } = string.Empty;
    public string NomeEmpresa { get; set; } = string.Empty;
    public string? NomeFormaPagamento { get; set; }
}

