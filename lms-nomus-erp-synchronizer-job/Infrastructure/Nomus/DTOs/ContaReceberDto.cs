using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

/// <summary>
/// DTO para resposta do endpoint de contas a receber do Nomus
/// </summary>
public class ContaReceberDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idPessoa")]
    public int IdPessoa { get; set; }

    [JsonPropertyName("idEmpresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("dataVencimento")]
    public object? DataVencimento { get; set; }

    [JsonPropertyName("dataCompetencia")]
    public object? DataCompetencia { get; set; }

    [JsonPropertyName("valorReceber")]
    public string ValorReceber { get; set; } = string.Empty;

    [JsonPropertyName("valorRecebido")]
    public string ValorRecebido { get; set; } = string.Empty;

    [JsonPropertyName("saldoReceber")]
    public string SaldoReceber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("classificacao")]
    public string Classificacao { get; set; } = string.Empty;

    [JsonPropertyName("nomePessoa")]
    public string NomePessoa { get; set; } = string.Empty;

    [JsonPropertyName("nomeEmpresa")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [JsonPropertyName("nomeFormaPagamento")]
    public string? NomeFormaPagamento { get; set; }
}

