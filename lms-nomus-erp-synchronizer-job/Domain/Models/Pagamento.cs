namespace lms_nomus_erp_synchronizer_job.Domain.Models;

/// <summary>
/// Representa um pagamento (baixa de conta a pagar) no ERP Nomus
/// </summary>
public class Pagamento
{
    public int Id { get; set; }
    public int IdContaPagar { get; set; }
    public DateTime DataPagamento { get; set; }
    public DateTime DataCompetencia { get; set; }
    public decimal ValorPago { get; set; }
    public decimal Desconto { get; set; }
    public decimal MultaJuros { get; set; }
    public bool BaixaContaPagar { get; set; }
    public string NomeFormaPagamento { get; set; } = string.Empty;
    public string NomeContaBancaria { get; set; } = string.Empty;
    public string NomePessoa { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
