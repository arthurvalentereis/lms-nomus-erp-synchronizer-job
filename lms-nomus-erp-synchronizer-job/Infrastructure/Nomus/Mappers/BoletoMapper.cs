using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

/// <summary>
/// Mapper para converter DTOs do Nomus em modelos de domínio
/// </summary>
public static class BoletoMapper
{
    public static Boleto ToDomain(BoletoDto dto)
    {
        return new Boleto
        {
            Id = dto.Id,
            IdContaReceber = dto.IdContaReceber,
            IdPessoa = dto.IdPessoa,
            IdEmpresa = dto.IdEmpresa,
            DataHoraEmissao = DateHelper.ParseDate(dto.DataHoraEmissao),
            DataVencimento = DateHelper.ParseDate(dto.DataVencimento),
            Valor = CurrencyHelper.ParseBrazilianCurrency(dto.Valor),
            Status = dto.Status,
            Cancelado = dto.Cancelado,
            NossoNumeroBoletoBancario = dto.NossoNumeroBoletoBancario,
            NumeroDocumento = dto.NumeroDocumento,
            NomePessoa = dto.NomePessoa,
            NomeEmpresa = dto.NomeEmpresa
        };
    }
}

