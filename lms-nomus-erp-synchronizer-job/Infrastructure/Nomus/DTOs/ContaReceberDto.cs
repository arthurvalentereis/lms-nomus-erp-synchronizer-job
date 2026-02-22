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

    [JsonPropertyName("idContaBancaria")]
    public int IdContaBancaria { get; set; }

    [JsonPropertyName("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [JsonPropertyName("idNfe")]
    public int IdNfe { get; set; }

    [JsonPropertyName("dataVencimento")]
    public string? DataVencimento { get; set; }

    [JsonPropertyName("dataCompetencia")]
    public string? DataCompetencia { get; set; }

    [JsonPropertyName("dataAgendamento")]
    public string? DataAgendamento { get; set; }

    [JsonPropertyName("dataHoraCriacao")]
    public string? DataHoraCriacao { get; set; }

    [JsonPropertyName("dataModificacao")]
    public string? DataModificacao { get; set; }

    [JsonPropertyName("valorReceber")]
    public string ValorReceber { get; set; } = string.Empty;

    [JsonPropertyName("valorRecebido")]
    public string ValorRecebido { get; set; } = string.Empty;

    [JsonPropertyName("saldoReceber")]
    public string SaldoReceber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public bool Status { get; set; } 

    [JsonPropertyName("tipo")]
    public int Tipo { get; set; }     

    [JsonPropertyName("classificacao")]
    public string Classificacao { get; set; } = string.Empty;

    [JsonPropertyName("nomePessoa")]
    public string NomePessoa { get; set; } = string.Empty;

    [JsonPropertyName("nomeEmpresa")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [JsonPropertyName("nomeFormaPagamento")]
    public string? NomeFormaPagamento { get; set; }

    [JsonPropertyName("numeroNotaFiscalOrigem")]
    public string? NumeroNotaFiscalOrigem { get; set; }

    [JsonPropertyName("cnpjPessoa")]
    public string? CnpjPessoa { get; set; }

    [JsonPropertyName("telefonePessoa")]
    public string? TelefonePessoa { get; set; }

    [JsonPropertyName("percentualMultaPorAtrasoEmContasReceber")]
    public string? PercentualMultaPorAtrasoEmContasReceber { get; set; }

    [JsonPropertyName("taxaMensalJuros")]
    public string? TaxaMensalJuros { get; set; }

    [JsonPropertyName("tipoCalculoMultaPorAtrasoEmContasReceber")]
    public string? TipoCalculoMultaPorAtrasoEmContasReceber { get; set; }

    [JsonPropertyName("tipoJurosAtrasoEmContasReceber")]
    public string? TipoJurosAtrasoEmContasReceber { get; set; }

    [JsonPropertyName("suspenderCobranca")]
    public bool SuspenderCobranca { get; set; }

    [JsonPropertyName("xmlNfe")]
    public string? XmlNfe { get; set; }
}

