using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

/// <summary>
/// DTO para resposta do endpoint de pagamentos do Nomus
/// </summary>
public class PagamentoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idContaPagar")]
    public int IdContaPagar { get; set; }

    [JsonPropertyName("dataPagamento")]
    public object? DataPagamento { get; set; }

    [JsonPropertyName("dataCompetencia")]
    public object? DataCompetencia { get; set; }

    [JsonPropertyName("valorPago")]
    public string ValorPago { get; set; } = string.Empty;

    [JsonPropertyName("desconto")]
    public string Desconto { get; set; } = string.Empty;

    [JsonPropertyName("multaJuros")]
    public string MultaJuros { get; set; } = string.Empty;

    [JsonPropertyName("baixaContaPagar")]
    public bool BaixaContaPagar { get; set; }

    [JsonPropertyName("nomeFormaPagamento")]
    public string NomeFormaPagamento { get; set; } = string.Empty;

    [JsonPropertyName("nomeContaBancaria")]
    public string NomeContaBancaria { get; set; } = string.Empty;

    [JsonPropertyName("nomePessoa")]
    public string NomePessoa { get; set; } = string.Empty;

    [JsonPropertyName("idEmpresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("descricaoLancamento")]
    public string DescricaoLancamento { get; set; } = string.Empty;
}
