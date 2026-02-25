using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers
{
    public static class CustomerMapper
    {
        public static Customer ToDomain(CustomerDto dto)
        {
            decimal? limite = null;
            if(dto.AnalisesCredito is not null)
                limite = Convert.ToInt32(dto.AnalisesCredito.FirstOrDefault()?.LimiteCredito);
            return new Customer
            {
                Id = dto.Id,
                Nome = dto.Nome,
                Cnpj = dto.Cnpj,
                Bairro = dto.Bairro,
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
                Ativo = dto.Ativo,
                CreditLimit = limite
            };
        }
    }
}
