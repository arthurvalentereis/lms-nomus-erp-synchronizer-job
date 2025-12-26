using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

/// <summary>
/// DTO para resposta do endpoint de boletos do Nomus
/// </summary>
public class BoletoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idContaReceber")]
    public int IdContaReceber { get; set; }

    [JsonPropertyName("idPessoa")]
    public int IdPessoa { get; set; }

    [JsonPropertyName("idEmpresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("dataHoraEmissao")]
    public object? DataHoraEmissao { get; set; }

    [JsonPropertyName("dataVencimento")]
    public object? DataVencimento { get; set; }

    [JsonPropertyName("valor")]
    public string Valor { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("cancelado")]
    public bool Cancelado { get; set; }

    [JsonPropertyName("nossoNumeroBoletoBancario")]
    public string NossoNumeroBoletoBancario { get; set; } = string.Empty;

    [JsonPropertyName("numeroDocumento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [JsonPropertyName("nomePessoa")]
    public string NomePessoa { get; set; } = string.Empty;

    [JsonPropertyName("nomeEmpresa")]
    public string NomeEmpresa { get; set; } = string.Empty;
}

