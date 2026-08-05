using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

namespace lms_nomus_erp_synchronizer_job.Application.Mappers;

/// <summary>
/// Mapper para converter dados de contas a pagar do Nomus em supplier invoices do Letmesee
/// </summary>
public static class SupplierInvoiceMapper
{
    /// <summary>
    /// Converte uma ContaPagar em RequestInvoice (payload reutilizado pelo Letmesee)
    /// </summary>
    public static RequestInvoiceDto ToInvoiceDto(
        ContaPagar contaPagar,
        List<Pagamento>? pagamentos = null,
        long? userGroupId = null,
        string? creditorDocument = null)
    {
        var pagamentoInfo = pagamentos?.FirstOrDefault(p => p.IdContaPagar == contaPagar.Id);

        var installmentSource = !string.IsNullOrWhiteSpace(pagamentoInfo?.Descricao)
            ? pagamentoInfo!.Descricao
            : contaPagar.DescricaoLancamento;

        var payDay = InvoiceMapper.VerifyIsMinimumDate(contaPagar.DataBaixa);
        if (payDay == null && pagamentoInfo != null)
            payDay = InvoiceMapper.VerifyIsMinimumDate(pagamentoInfo.DataPagamento);

        var invoice = new RequestInvoiceDto
        {
            UserGroupId = userGroupId,
            InvoiceNumber = contaPagar.Id.ToString(),
            InvoiceValue = contaPagar.ValorReceber,
            InvoiceDueDate = contaPagar.DataVencimento,
            IssueDate = contaPagar.DataCompetencia,
            BuyerName = contaPagar.NomePessoa,
            BuyerDocument = InvoiceMapper.LimparDocumento(contaPagar.CnpjCpfPessoa ?? ""),
            BuyerId = contaPagar.IdPessoa.ToString(),
            Description = $"Conta a Pagar {contaPagar.Id} - {contaPagar.Classificacao}",
            Type = contaPagar.Tipo.ToString(),
            WasPaid = contaPagar.Status == true,
            PayDay = payDay,
            InvoiceInstallment = InvoiceMapper.ObterNumeroParcela(installmentSource) ?? 1,
            CreditorDocument = creditorDocument ?? string.Empty
        };

        if (pagamentoInfo != null)
        {
            invoice.Discount = pagamentoInfo.Desconto;
            invoice.Fees = pagamentoInfo.MultaJuros;
        }

        return invoice;
    }

    /// <summary>
    /// Converte uma lista de ContasPagar em RequestInvoices
    /// </summary>
    public static IEnumerable<RequestInvoiceDto> ToInvoiceDtos(
        IEnumerable<ContaPagar> contasPagar,
        IEnumerable<Pagamento> pagamentos,
        long? userGroupId = null,
        string? creditorDocument = null)
    {
        var pagamentosList = pagamentos.ToList();

        return contasPagar.Select(conta =>
            ToInvoiceDto(conta, pagamentosList, userGroupId, creditorDocument)).ToList();
    }
}
