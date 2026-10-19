using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

/// <summary>
/// Mapper para converter DTOs de pessoas do Nomus em suppliers de domínio
/// </summary>
public static class PessoaMapper
{
    public static Supplier ToDomain(PessoaDto dto)
    {
        var documento = !string.IsNullOrWhiteSpace(dto.Cnpj)
            ? dto.Cnpj
            : (dto.Cpf ?? string.Empty);

        return new Supplier
        {
            Id = dto.Id,
            Nome = dto.Nome,
            Cnpj = documento,
            Bairro = dto.BairroDistrito,
            Endereco = dto.Endereco,
            Numero = dto.Numero,
            Municipio = dto.Municipio,
            Pais = dto.Pais,
            Cep = dto.Cep,
            Complemento = dto.Complemento,
            Uf = dto.Uf,
            DataCriacao = DateHelper.ParseDate(dto.DataCriacao),
            Telefone = dto.Telefone,
            Email = dto.Email,
            Ativo = dto.Ativo
        };
    }
}
