using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

/// <summary>
/// Mapper para converter DTOs do Nomus em modelos de domínio
/// </summary>
public static class ContaReceberMapper
{
    public static ContaReceber ToDomain(ContaReceberDto dto)
    {
        return new ContaReceber
        {
            Id = dto.Id,
            IdPessoa = dto.IdPessoa,
            IdEmpresa = dto.IdEmpresa,
            DataVencimento = DateHelper.ParseDate(dto.DataVencimento),
            DataCompetencia = DateHelper.ParseDate(dto.DataCompetencia),
            ValorReceber = CurrencyHelper.ParseBrazilianCurrency(dto.ValorReceber),
            ValorRecebido = CurrencyHelper.ParseBrazilianCurrency(dto.ValorRecebido),
            SaldoReceber = CurrencyHelper.ParseBrazilianCurrency(dto.SaldoReceber),
            Status = dto.Status,
            Tipo = dto.Tipo,
            Classificacao = dto.Classificacao,
            NomePessoa = dto.NomePessoa,
            CnpjCpfPessoa= dto.CnpjPessoa,
            NomeEmpresa = dto.NomeEmpresa,
            NomeFormaPagamento = dto.NomeFormaPagamento
        };
    }
}

