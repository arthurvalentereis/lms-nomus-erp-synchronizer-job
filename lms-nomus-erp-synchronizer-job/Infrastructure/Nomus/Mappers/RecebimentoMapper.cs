using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

/// <summary>
/// Mapper para converter DTOs do Nomus em modelos de domínio
/// </summary>
public static class RecebimentoMapper
{
    public static Recebimento ToDomain(RecebimentoDto dto)
    {
        return new Recebimento
        {
            Id = dto.Id,
            IdContaReceber = dto.IdContaReceber,
            DataRecebimento = DateHelper.ParseDate(dto.DataRecebimento),
            DataCompetencia = DateHelper.ParseDate(dto.DataCompetencia),
            ValorRecebido = CurrencyHelper.ParseBrazilianCurrency(dto.ValorRecebido),
            Desconto = CurrencyHelper.ParseBrazilianCurrency(dto.Desconto),
            MultaJuros = CurrencyHelper.ParseBrazilianCurrency(dto.MultaJuros),
            BaixaContaReceber = dto.BaixaContaReceber,
            NomeFormaPagamento = dto.NomeFormaPagamento,
            NomeContaBancaria = dto.NomeContaBancaria,
            NomePessoa = dto.NomePessoa,
            IdEmpresa = dto.IdEmpresa,
            Descricao = dto.DescricaoLancamento
        };
    }
}

