using Hangfire;
using lms_nomus_erp_synchronizer_job.Application.Mappers;
using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee;
using lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;
using lms_nomus_erp_synchronizer_job.Worker.Jobs;
using System.Text;

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

    public async Task SynchronizeClienteAsync(long userGroupId, long userCompanyId, string creditorDocument, string hashToken, string baseUrl, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserGroupId"] = userGroupId
        });

        _logger.LogInformation("Iniciando sincronização do cliente {UserGroupId}", userGroupId);

        // Criar cliente Nomus com token específico
        var nomusClient = _nomusClientFactory.CreateClient(hashToken,baseUrl);
        
        //Vou criar o get dos customers aqui 
        var customerTask = FetchCustomersAsync(nomusClient, userGroupId, cancellationToken);


        // Buscar dados do Nomus em paralelo
        var boletosTask = FetchBoletosAsync(nomusClient, userGroupId, cancellationToken);
        var recebimentosTask = FetchRecebimentosAsync(nomusClient, userGroupId, cancellationToken);
        var contasTask = FetchContasReceberAsync(nomusClient, userGroupId, cancellationToken);

        await Task.WhenAll(customerTask, boletosTask, recebimentosTask, contasTask);
        //await Task.WhenAll(customerTask);

        var boletos = await boletosTask;
        var recebimentos = await recebimentosTask;
        var contasReceber = await contasTask;
        var customers = await customerTask;

        _logger.LogInformation(
           "Dados recebidos do Nomus para cliente {UserGroupId}: Boletos: {BoletoCount}, Recebimentos: {RecebimentoCount}, Contas: {ContaCount}",
          userGroupId, boletos.Count, recebimentos.Count, contasReceber.Count);

       // Enviar invoices para o Letmesee em batches
       await SendInvoicesInBatchesAsync(boletos, recebimentos, contasReceber, userGroupId,creditorDocument, cancellationToken);  
        
        // Enviar customers para o Letmesee em batches
        await SendCustomerInBatchesAsync(customers, userGroupId,userCompanyId, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Sincronização do cliente {UserGroupId} concluída. Duração: {Duration}ms",
            userGroupId, duration.TotalMilliseconds);

        // ao termino agendo o proximo para daqui a 5 minutos
        BackgroundJob.Schedule<ScheduleSyncJob>(
        x => x.ExecuteAsync(CancellationToken.None),
        TimeSpan.FromMinutes(5));
    }
    public async Task SynchronizeAllFilesClienteAsync(long userGroupId, long userCompanyId, string creditorDocument, string hashToken, string baseUrl, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserGroupId"] = userGroupId
        });

        _logger.LogInformation("Iniciando sincronização do cliente {UserGroupId}", userGroupId);

        // Criar cliente Nomus com token específico
        var nomusClient = _nomusClientFactory.CreateClient(hashToken, baseUrl);

        //Vou criar o get dos customers aqui 
        var customerTask = FetchAllCustomersAsync(nomusClient, userGroupId, cancellationToken);


        // Buscar dados do Nomus em paralelo
        var boletosTask = FetchAllBoletosAsync(nomusClient, userGroupId, cancellationToken);
        var recebimentosTask = FetchAllRecebimentosAsync(nomusClient, userGroupId, cancellationToken);
        var contasTask = FetchAllContasReceberAsync(nomusClient, userGroupId, cancellationToken);

        await Task.WhenAll(customerTask, boletosTask, recebimentosTask, contasTask);
        //await Task.WhenAll(contasTask);

        var boletos = await boletosTask;
        var recebimentos = await recebimentosTask;
        var contasReceber = await contasTask;
        var customers = await customerTask;

        _logger.LogInformation(
           "Dados recebidos do Nomus para cliente {UserGroupId}: Boletos: {BoletoCount}, Recebimentos: {RecebimentoCount}, Contas: {ContaCount}",
          userGroupId, boletos.Count, recebimentos.Count, contasReceber.Count);

        // Enviar invoices para o Letmesee em batches
        await SendInvoicesInBatchesAsync(boletos, recebimentos, contasReceber, userGroupId, creditorDocument, cancellationToken);

        // Enviar customers para o Letmesee em batches
        await SendCustomerInBatchesAsync(customers, userGroupId, userCompanyId, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Sincronização do cliente {UserGroupId} concluída. Duração: {Duration}ms",
            userGroupId, duration.TotalMilliseconds);
    }

    #region PrivatesAllFiles
    private async Task<List<Customer>> FetchAllCustomersAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando customers do cliente {UserGroupId}", userGroupId);

            var ontem = DateTime.Now.AddDays(-1);
            var dataFiltro = ontem.ToString("dd/MM/yyyy"); // formato comum no Nomus

            var filtro = $"ativo=true";
            var encoded = Uri.EscapeDataString(filtro);
            var url = $"rest/clientes?query={encoded}";
            var customerDto = await nomusClient.GetAllCustomerAsync(url, cancellationToken);
            var customer = customerDto.Select(Infrastructure.Nomus.Mappers.CustomerMapper.ToDomain).ToList();

            _logger.LogInformation("Total de customers recebidos para cliente {UserGroupId}: {Count}",
                userGroupId, customer.Count);

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar boletos do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }
    private async Task<List<Boleto>> FetchAllBoletosAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando boletos do cliente {UserGroupId}", userGroupId);
            var url = $"rest/boletos";
           // var boletosDto = await nomusClient.GetAllBoletosAsync(url, cancellationToken);
            //var boletos = boletosDto.Select(BoletoMapper.ToDomain).ToList();

            //_logger.LogInformation("Total de boletos recebidos para cliente {UserGroupId}: {Count}",
              //  userGroupId, boletos.Count);

            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar boletos do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }

    private async Task<List<Recebimento>> FetchAllRecebimentosAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando recebimentos do cliente {UserGroupId}", userGroupId);
            var url = $"rest/recebimentos";
            var recebimentosDto = await nomusClient.GetAllRecebimentosAsync(url, cancellationToken);
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

    private async Task<List<ContaReceber>> FetchAllContasReceberAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando contas a receber do cliente {UserGroupId}", userGroupId);
            var url = $"rest/contasReceber";
            var contasDto = await nomusClient.GetAllContasReceberAsync(url, cancellationToken);
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
    #endregion

    #region Privates
    private async Task<List<Customer>> FetchCustomersAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando customers do cliente {UserGroupId}", userGroupId);
            var ontem = DateTime.Now.AddDays(-1);
            var dataFiltro = ontem.ToString("dd/MM/yyyy"); // formato comum no Nomus

            var filtro = $"ativo=true;dataCriacao>={dataFiltro}";
            var encoded = Uri.EscapeDataString(filtro);
            var url = $"rest/clientes?query={encoded}";
            var customerDto = await nomusClient.GetCustomerAsync(url, cancellationToken);
            var customer = customerDto.Select(Infrastructure.Nomus.Mappers.CustomerMapper.ToDomain).ToList();

            _logger.LogInformation("Total de customers recebidos para cliente {UserGroupId}: {Count}",
                userGroupId, customer.Count);

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar boletos do cliente {UserGroupId}", userGroupId);
            throw;
        }
    }
    private async Task<List<Boleto>> FetchBoletosAsync(
        INomusClient nomusClient,
        long userGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Buscando boletos do cliente {UserGroupId}", userGroupId);
            var ontem = DateTime.Now.AddDays(-1);
            var dataFiltro = ontem.ToString("dd/MM/yyyy"); // formato comum no Nomus
            var filtro = $"dataHoraEmissao>={dataFiltro}";
            var encoded = Uri.EscapeDataString(filtro);
            var url = $"rest/boletos?query={encoded}";
            //var boletosDto = await nomusClient.GetBoletosAsync(url, cancellationToken);
            //var boletos = boletosDto.Select(BoletoMapper.ToDomain).ToList();

            //_logger.LogInformation("Total de boletos recebidos para cliente {UserGroupId}: {Count}",
               // userGroupId, boletos.Count);

            return [];
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
            var ontem = DateTime.Now.AddDays(-1);
            var dataFiltro = ontem.ToString("dd/MM/yyyy"); // formato comum no Nomus
            var filtro = $"dataRecebimento>={dataFiltro}";
            var encoded = Uri.EscapeDataString(filtro);
            var url = $"rest/recebimentos?query={encoded}";
            var recebimentosDto = await nomusClient.GetRecebimentosAsync(url, cancellationToken);
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
            var ontem = DateTime.Now.AddDays(-1);
            var dataFiltro = ontem.ToString("dd/MM/yyyy"); // formato comum no Nomus
            var filtro = $"dataCompetencia>={dataFiltro}";
            var encoded = Uri.EscapeDataString(filtro);
            var url = $"rest/contasReceber?query={encoded}";
            var contasDto = await nomusClient.GetContasReceberAsync(url, cancellationToken);
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
    #endregion



    /// <summary>
    /// Envia invoices para o Letmesee em batches para evitar problemas de tamanho e timeout
    /// </summary>
    private async Task SendInvoicesInBatchesAsync(
        List<Boleto> boletos,
        List<Recebimento> recebimentos,
        List<ContaReceber> contasReceber,
        long userGroupId,
        string creditorDocument,
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
                userGroupId,
                creditorDocument
                ).ToList();

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
    /// <summary>
    /// Envia customers para o letmesee.
    /// </summary>
    private async Task SendCustomerInBatchesAsync(
        List<Customer> customer,
        long userGroupId,
        long userCompanyId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Convertendo dados para customers do Letmesee para cliente {UserGroupId}", userGroupId);

            // Converter dados do Nomus em invoices do Letmesee
            var customers = customer.Select(customer =>
            {
                return Mappers.CustomerMapper.ToCustomerDto(customer, userGroupId,userCompanyId);
            }).ToList();

            if (!customers.Any())
            {
                _logger.LogInformation("Nenhuma customers para enviar ao Letmesee para cliente {UserGroupId}", userGroupId);
                return;
            }

            // Enviar todas as invoices de uma vez (Letmesee deve suportar o batch)
            // Se necessário, pode ser dividido em batches menores
            _logger.LogInformation(
                "Enviando {Count} customers para o Letmesee (cliente {UserGroupId})",
                customers.Count, userGroupId);

            await _letmeseeService.SendCustomerAsync(customers, cancellationToken);

            _logger.LogInformation("Customers enviados com sucesso para o Letmesee (cliente {UserGroupId})", userGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar customers para o Letmesee (cliente {UserGroupId})", userGroupId);
            throw;
        }
    }
}
