using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

namespace lms_nomus_erp_synchronizer_job.Application.Mappers
{
    public class CustomerMapper
    {
        public static string LimparDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return string.Empty;

            return new string(documento.Where(char.IsDigit).ToArray());
        }
        /// <summary>
        /// Converte uma ContaReceber em RequestInvoice
        /// </summary>
        public static RequestCustomerDto ToCustomerDto(
            Customer customerDto,
            long? userGroupId = null,
            long? userCompanyId = null)
        {

            var customer = new RequestCustomerDto
            {
                UserGroupId = userGroupId,
                UserCompanyId = userCompanyId,
                Name = customerDto.Nome,
                Cnpj = LimparDocumento(customerDto.Cnpj ?? ""),
                Email = customerDto.Email,
                PhoneNumber = customerDto.Telefone,
                Address = customerDto.Endereco,
                Neighborhood = customerDto.Bairro,
                ZipCode = customerDto.Cep,
                City = customerDto.Municipio,
                State = customerDto.Uf,
                Number = customerDto.Numero,
                Complement = customerDto.Complemento,
                CreditLimit = customerDto.CreditLimit,
                RegistratedAt = customerDto.DataCriacao


            };
            return customer;
        }
    }
}
