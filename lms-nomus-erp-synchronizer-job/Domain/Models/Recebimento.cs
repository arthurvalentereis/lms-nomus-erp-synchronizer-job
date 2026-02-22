namespace lms_nomus_erp_synchronizer_job.Domain.Models;

/// <summary>
/// Representa um recebimento no ERP Nomus
/// </summary>
public class Recebimento
{
    public int Id { get; set; }
    public int IdContaReceber { get; set; }
    public DateTime DataRecebimento { get; set; }
    public DateTime DataCompetencia { get; set; }
    public decimal ValorRecebido { get; set; }
    public decimal Desconto { get; set; }
    public decimal MultaJuros { get; set; }
    public bool BaixaContaReceber { get; set; }
    public string NomeFormaPagamento { get; set; } = string.Empty;
    public string NomeContaBancaria { get; set; } = string.Empty;
    public string NomePessoa { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
    public string Descricao { get; set; } = string.Empty;
}

