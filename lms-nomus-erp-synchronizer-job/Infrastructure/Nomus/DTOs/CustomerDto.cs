using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs
{
    public class CustomerDto
    {
        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; } = string.Empty;

        [JsonPropertyName("cep")]
        public string Cep { get; set; } = string.Empty;

        [JsonPropertyName("cnpj")]
        public string Cnpj { get; set; } = string.Empty;

        [JsonPropertyName("classificacao")]
        public string Classificacao { get; set; } = string.Empty;

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("codigoIBGEMunicipio")]
        public string CodigoIBGEMunicipio { get; set; } = string.Empty;

        [JsonPropertyName("codigoSistemaContabil")]
        public string CodigoSistemaContabil { get; set; } = string.Empty;

        [JsonPropertyName("complemento")]
        public string Complemento { get; set; } = string.Empty;

        [JsonPropertyName("crt")]
        public int Crt { get; set; }

        [JsonPropertyName("dataCriacao")]
        public string? DataCriacao { get; set; }

        [JsonPropertyName("dataEmissaoUltimoPedidoVenda")]
        public string? DataEmissaoUltimoPedidoVenda { get; set; }

        [JsonPropertyName("dataInicioRelacionamento")]
        public string? DataInicioRelacionamento { get; set; }

        [JsonPropertyName("dataModificacao")]
        public string? DataModificacao { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("endereco")]
        public string Endereco { get; set; } = string.Empty;

        [JsonPropertyName("enderecos")]
        public List<object>? Enderecos { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("inscricaoEstadual")]
        public string InscricaoEstadual { get; set; } = string.Empty;

        [JsonPropertyName("municipio")]
        public string Municipio { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("numero")]
        public string Numero { get; set; } = string.Empty;

        [JsonPropertyName("observacoes")]
        public string Observacoes { get; set; } = string.Empty;

        [JsonPropertyName("pais")]
        public string Pais { get; set; } = string.Empty;

        [JsonPropertyName("site")]
        public string Site { get; set; } = string.Empty;

        [JsonPropertyName("suspenderCobranca")]
        public bool SuspenderCobranca { get; set; }

        [JsonPropertyName("telefone")]
        public string Telefone { get; set; } = string.Empty;

        [JsonPropertyName("telefoneNeofinDDD")]
        public string TelefoneNeofinDDD { get; set; } = string.Empty;

        [JsonPropertyName("telefoneNeofinDDI")]
        public string TelefoneNeofinDDI { get; set; } = string.Empty;

        [JsonPropertyName("telefoneNeofinNumero")]
        public string TelefoneNeofinNumero { get; set; } = string.Empty;

        [JsonPropertyName("tipoContribuinteICMS")]
        public int TipoContribuinteICMS { get; set; }

        [JsonPropertyName("tipoLogradouro")]
        public string TipoLogradouro { get; set; } = string.Empty;

        [JsonPropertyName("tipoPessoa")]
        public int TipoPessoa { get; set; }

        [JsonPropertyName("uf")]
        public string Uf { get; set; } = string.Empty;

        [JsonPropertyName("analisesCredito")]
        public List<CustomerAnalysisDto> AnalisesCredito { get; set; }
    }
    public class CustomerAnalysisDto
    {
        [JsonPropertyName("dataAnaliseCredito")]
        public string DataAnaliseCredito { get; set; }
        [JsonPropertyName("decisaoAnaliseCredito")]
        public long DecisaoAnaliseCredito { get; set; }
        [JsonPropertyName("limiteCredito")]
        public string? LimiteCredito { get; set; }
        [JsonPropertyName("limiteCreditoInadimplenciaDiasAtraso")]
        public string LimiteCreditoInadimplenciaDiasAtraso { get; set; } = string.Empty;
        [JsonPropertyName("limiteInadimplenciaValor")]
        public string LimiteInadimplenciaValor { get; set; } = string.Empty;

    }
}
