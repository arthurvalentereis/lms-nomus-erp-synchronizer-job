namespace lms_nomus_erp_synchronizer_job.Domain.Models;

/// <summary>
/// Representa uma pessoa/fornecedor no ERP Nomus (rest/pessoas)
/// </summary>
public class Supplier
{
    public long Id { get; set; }
    public bool Ativo { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
}
