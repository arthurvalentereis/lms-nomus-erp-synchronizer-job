using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

namespace lms_nomus_erp_synchronizer_job.Application.Mappers;

/// <summary>
/// Mapper para converter dados do Nomus em invoices do Letmesee
/// </summary>
public static class InvoiceMapper
{
    /// <summary>
    /// Converte uma ContaReceber em RequestInvoice
    /// </summary>
    public static RequestInvoiceDto ToInvoiceDto(
        ContaReceber contaReceber,
        Boleto? boleto = null,
        Recebimento? recebimento = null,
        long? userGroupId = null)
    {
        var invoice = new RequestInvoiceDto
        {
            UserGroupId = userGroupId,
            InvoiceNumber = boleto?.NumeroDocumento ?? contaReceber.Id.ToString(),
            InvoiceValue = contaReceber.ValorReceber,
            InvoiceDueDate = contaReceber.DataVencimento,
            IssueDate = contaReceber.DataCompetencia,
            BuyerName = contaReceber.NomePessoa,
            BuyerId = contaReceber.IdPessoa.ToString(),
            Description = $"Conta a Receber {contaReceber.Id} - {contaReceber.Classificacao}",
            Type = contaReceber.Tipo,
            Discount = recebimento?.Desconto ?? 0,
            Fees = recebimento?.MultaJuros ?? 0,
            OurNumber = boleto?.NossoNumeroBoletoBancario,
            ManagementOfDefaulters = contaReceber.Status != "Pago",
            InExtract = contaReceber.Status == "Pago"
        };

        // Se houver boleto, adiciona informações adicionais
        if (boleto != null)
        {
            invoice.BankSlipUrl = !string.IsNullOrEmpty(boleto.NossoNumeroBoletoBancario)
                ? $"boleto/{boleto.NossoNumeroBoletoBancario}"
                : null;
        }

        // Se houver recebimento, adiciona informações de pagamento
        if (recebimento != null)
        {
            invoice.Discount = recebimento.Desconto;
            invoice.Fees = recebimento.MultaJuros;
        }

        return invoice;
    }

    /// <summary>
    /// Converte uma lista de ContasReceber em RequestInvoices
    /// </summary>
    public static IEnumerable<RequestInvoiceDto> ToInvoiceDtos(
        IEnumerable<ContaReceber> contasReceber,
        IEnumerable<Boleto> boletos,
        IEnumerable<Recebimento> recebimentos,
        long? userGroupId = null)
    {
        var boletosDict = boletos.ToDictionary(b => b.IdContaReceber);
        var recebimentosDict = recebimentos.GroupBy(r => r.IdContaReceber)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());

        return contasReceber.Select(conta =>
        {
            boletosDict.TryGetValue(conta.Id, out var boleto);
            recebimentosDict.TryGetValue(conta.Id, out var recebimento);
            return ToInvoiceDto(conta, boleto, recebimento, userGroupId);
        }).ToList();
    }
}

