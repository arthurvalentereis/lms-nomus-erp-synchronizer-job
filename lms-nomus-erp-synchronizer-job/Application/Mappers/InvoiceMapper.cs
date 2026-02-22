using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using System.Text.RegularExpressions;

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
        List<Boleto>? boleto = null,
        List<Recebimento>? recebimento = null,
        long? userGroupId = null)
    {
        var boletoInfo = boleto?.FirstOrDefault(b => b.IdContaReceber == contaReceber.Id);
        var recebimentoInfo = recebimento?.FirstOrDefault(r => r.IdContaReceber == contaReceber.Id);

        var invoice = new RequestInvoiceDto
        {
            UserGroupId = userGroupId,
            InvoiceNumber = contaReceber.Id.ToString(),
            InvoiceValue = contaReceber.ValorReceber,
            InvoiceDueDate = contaReceber.DataVencimento,
            IssueDate = contaReceber.DataCompetencia,
            BuyerName = contaReceber.NomePessoa,
            BuyerDocument = LimparDocumento(contaReceber.CnpjCpfPessoa ?? ""),
            BuyerId = contaReceber.IdPessoa.ToString(),
            Description = $"Conta a Receber {contaReceber.Id} - {contaReceber.Classificacao}",
            Type = contaReceber.Tipo.ToString(),
            InExtract = contaReceber.Status == true,
            InvoiceInstallment = ObterNumeroParcela(recebimentoInfo?.Descricao ?? "") ?? 1

        };

        // Se houver boleto, adiciona informações adicionais
        if (boletoInfo != null)
        {
            invoice.BankSlipUrl = !string.IsNullOrEmpty(boletoInfo.NossoNumeroBoletoBancario)
                ? $"boleto/{boletoInfo.NossoNumeroBoletoBancario}"
                : null;
        }

        // Se houver recebimento, adiciona informações de pagamento
        if (recebimentoInfo != null)
        {
            invoice.Discount = recebimentoInfo.Desconto;
            invoice.Fees = recebimentoInfo.MultaJuros;
        }

        return invoice;
    }
    public static int? ObterNumeroParcela(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return null;

        var match = Regex.Match(descricao, @"Parcela\s+(\d+)", RegexOptions.IgnoreCase);

        if (match.Success)
            return int.Parse(match.Groups[1].Value);

        return null;
    }
    public static string LimparDocumento(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return string.Empty;

        return new string(documento.Where(char.IsDigit).ToArray());
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
        var boletosDict = boletos.GroupBy(r => r.IdContaReceber)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        //var boletosDict = boletos.ToDictionary(b => b.IdContaReceber);
        var recebimentosDict = recebimentos.GroupBy(r => r.IdContaReceber)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());

        return contasReceber.Select(conta =>
        {
            return ToInvoiceDto(conta, boletos.ToList(), recebimentos.ToList(), userGroupId);
        }).ToList();
    }
}

