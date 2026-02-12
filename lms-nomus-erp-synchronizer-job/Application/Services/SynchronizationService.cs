using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Implementação do serviço de sincronização
/// Processa APENAS 1 cliente por vez
/// Fluxo:
/// 1. Busca dados do Nomus usando o token do cliente
/// 2. Converte para formato do Letmesee
/// 3. Envia invoices para o Letmesee
/// </summary>
public class SynchronizationService : ISynchronizationService
{
    private readonly INomusClientFactory _nomusClientFactory;
    private readonly ILetmeseeService _letmeseeService;
    private readonly ILogger<SynchronizationService> _logger;

    public SynchronizationService(
        INomusClientFactory nomusClientFactory,
        ILetmeseeService letmeseeService,
        ILogger<SynchronizationService> logger)
    {
        _nomusClientFactory = nomusClientFactory;
        _letmeseeService = letmeseeService;
        _logger = logger;
    }

    public async Task SynchronizeClienteAsync(long userGroupId, string hashToken, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserGroupId"] = userGroupId
        });

        _logger.LogInformation("Iniciando sincronização do cliente {UserGroupId}", userGroupId);

        // Criar cliente Nomus com token específico
        var nomusClient = _nomusClientFactory.CreateClient(hashToken);

        // Buscar dados do Nomus em paralelo
        var boletosTask = FetchBoletosAsync(nomusClient, userGroupId, cancellationToken);
        var recebimentosTask = FetchRecebimentosAsync(nomusClient, userGroupId, cancellationToken);
        var contasTask = FetchContasReceberAsync(nomusClient, userGroupId, cancellationToken);

        await Task.WhenAll(boletosTask, recebimentosTask, contasTask);

        var boletos = await boletosTask;
        var recebimentos = await recebimentosTask;
        var contasReceber = await contasTask;

        _logger.LogInformation(
            "Dados recebidos do Nomus para cliente {UserGroupId}: Boletos: {BoletoCount}, Recebimentos: {RecebimentoCount}, Contas: {ContaCount}",
            userGroupId, boletos.Count, recebimentos.Count, contasReceber.Count);

        // Enviar invoices para o Letmesee em batches
        await SendInvoicesInBatchesAsync(boletos, recebimentos, contasReceber, userGroupId, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Sincronização do cliente {UserGroupId} concluída. Duração: {Duration}ms",
            userGroupId, duration.TotalMilliseconds);
    }

    private async Task<List<Boleto>> FetchBoletosAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando boletos do cliente {UserGroupId}", userGroupId);

            var boletosDto = await nomusClient.GetBoletosAsync(cancellationToken);
            var boletos = boletosDto.Select(BoletoMapper.ToDomain).ToList();

            _logger.LogInformation("Total de boletos recebidos para cliente {UserGroupId}: {Count}",
                userGroupId, boletos.Count);

            return boletos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar boletos do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }

    private async Task<List<Recebimento>> FetchRecebimentosAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando recebimentos do cliente {UserGroupId}", userGroupId);

            var recebimentosDto = await nomusClient.GetRecebimentosAsync(cancellationToken);
            var recebimentos = recebimentosDto.Select(RecebimentoMapper.ToDomain).ToList();

            _logger.LogInformation("Total de recebimentos recebidos para cliente {UserGroupId}: {Count}",
                userGroupId, recebimentos.Count);

            return recebimentos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar recebimentos do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }

    private async Task<List<ContaReceber>> FetchContasReceberAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando contas a receber do cliente {UserGroupId}", userGroupId);

            var contasDto = await nomusClient.GetContasReceberAsync(cancellationToken);
            var contas = contasDto.Select(ContaReceberMapper.ToDomain).ToList();

            _logger.LogInformation("Total de contas a receber recebidas para cliente {UserGroupId}: {Count}",
                userGroupId, contas.Count);

            return contas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar contas a receber do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }

    /// <summary>
    /// Envia invoices para o Letmesee em batches para evitar problemas de tamanho e timeout
    /// </summary>
    private async Task SendInvoicesInBatchesAsync(
        List<Boleto> boletos,
        List<Recebimento> recebimentos,
        List<ContaReceber> contasReceber,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Convertendo dados para invoices do Letmesee para cliente {UserGroupId}", userGroupId);

            // Converter dados do Nomus em invoices do Letmesee
            var invoices = InvoiceMapper.ToInvoiceDtos(
                contasReceber,
                boletos,
                recebimentos,
                userGroupId).ToList();

            if (!invoices.Any())
            {
                _logger.LogInformation("Nenhuma invoice para enviar ao Letmesee para cliente {UserGroupId}", userGroupId);
                return;
            }

            // Enviar todas as invoices de uma vez (Letmesee deve suportar o batch)
            // Se necessário, pode ser dividido em batches menores
            _logger.LogInformation(
                "Enviando {Count} invoices para o Letmesee (cliente {UserGroupId})",
                invoices.Count, userGroupId);

            await _letmeseeService.SendInvoicesAsync(invoices, cancellationToken);

            _logger.LogInformation("Invoices enviadas com sucesso para o Letmesee (cliente {UserGroupId})", userGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar invoices para o Letmesee (cliente {UserGroupId})", userGroupId);
            throw;
        }
    }
}
