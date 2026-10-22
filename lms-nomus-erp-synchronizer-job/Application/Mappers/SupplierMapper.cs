using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

namespace lms_nomus_erp_synchronizer_job.Application.Mappers;

/// <summary>
/// Mapper para converter suppliers (pessoas Nomus) no payload de customer/supplier do Letmesee
/// </summary>
public static class SupplierMapper
{
    public static string LimparDocumento(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return string.Empty;

        return new string(documento.Where(char.IsDigit).ToArray());
    }

    public static RequestCustomerDto ToSupplierDto(
        Supplier supplier,
        long? userGroupId = null,
        long? userCompanyId = null)
    {
        return new RequestCustomerDto
        {
            UserGroupId = userGroupId,
            UserCompanyId = userCompanyId,
            Name = supplier.Nome,
            Cnpj = LimparDocumento(supplier.Cnpj ?? ""),
            InternalCode = supplier.Id > 0 ? supplier.Id.ToString() : null,
            Email = supplier.Email,
            PhoneNumber = supplier.Telefone,
            Address = supplier.Endereco,
            Neighborhood = supplier.Bairro,
            ZipCode = supplier.Cep,
            City = supplier.Municipio,
            State = supplier.Uf,
            Number = supplier.Numero,
            Complement = supplier.Complemento,
            RegistratedAt = supplier.DataCriacao
        };
    }
}
