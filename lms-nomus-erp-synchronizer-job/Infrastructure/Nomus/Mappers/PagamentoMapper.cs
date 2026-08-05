using lms_nomus_erp_synchronizer_job.Domain.Helpers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.DTOs;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

/// <summary>
/// Mapper para converter DTOs de pagamentos do Nomus em modelos de domínio
/// </summary>
public static class PagamentoMapper
{
    public static Pagamento ToDomain(PagamentoDto dto)
    {
        return new Pagamento
        {
            Id = dto.Id,
            IdContaPagar = dto.IdContaPagar,
            DataPagamento = DateHelper.ParseDate(dto.DataPagamento),
            DataCompetencia = DateHelper.ParseDate(dto.DataCompetencia),
            ValorPago = CurrencyHelper.ParseBrazilianCurrency(dto.ValorPago),
            Desconto = CurrencyHelper.ParseBrazilianCurrency(dto.Desconto),
            MultaJuros = CurrencyHelper.ParseBrazilianCurrency(dto.MultaJuros),
            BaixaContaPagar = dto.BaixaContaPagar,
            NomeFormaPagamento = dto.NomeFormaPagamento,
            NomeContaBancaria = dto.NomeContaBancaria,
            NomePessoa = dto.NomePessoa,
            IdEmpresa = dto.IdEmpresa,
            Descricao = dto.DescricaoLancamento
        };
    }
}
