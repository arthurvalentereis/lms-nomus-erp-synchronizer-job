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

    [JsonPropertyName("idContaBancaria")]
    public int IdContaBancaria { get; set; }

    [JsonPropertyName("idConvenioBancario")]
    public int IdConvenioBancario { get; set; }

    [JsonPropertyName("idEmpresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("idPessoa")]
    public int IdPessoa { get; set; }

    [JsonPropertyName("idNfeOrigem")]
    public int IdNfeOrigem { get; set; }

    [JsonPropertyName("cancelado")]
    public bool Cancelado { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; } 

    [JsonPropertyName("classificacaoContaReceber")]
    public string ClassificacaoContaReceber { get; set; } = string.Empty;

    [JsonPropertyName("descricaoLancamentoContaReceber")]
    public string DescricaoLancamentoContaReceber { get; set; } = string.Empty;

    [JsonPropertyName("codigoBarras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [JsonPropertyName("linhaDigitavel")]
    public string LinhaDigitavel { get; set; } = string.Empty;

    [JsonPropertyName("nossoNumeroBoletoBancario")]
    public string NossoNumeroBoletoBancario { get; set; } = string.Empty;

    [JsonPropertyName("numeroDocumento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [JsonPropertyName("dataHoraEmissao")]
    public string DataHoraEmissao { get; set; } = string.Empty;

    [JsonPropertyName("dataVencimento")]
    public string DataVencimento { get; set; } = string.Empty;

    [JsonPropertyName("nomeContaBancaria")]
    public string NomeContaBancaria { get; set; } = string.Empty;

    [JsonPropertyName("nomeConvenioBancario")]
    public string NomeConvenioBancario { get; set; } = string.Empty;

    [JsonPropertyName("nomeEmpresa")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [JsonPropertyName("nomePessoa")]
    public string NomePessoa { get; set; } = string.Empty;

    [JsonPropertyName("valor")]
    public string Valor { get; set; } = string.Empty;


}

