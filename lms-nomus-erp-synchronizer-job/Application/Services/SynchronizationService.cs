using lms_nomus_erp_synchronizer_job.Domain.Models;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus;
using lms_nomus_erp_synchronizer_job.Infrastructure.Nomus.Mappers;
using lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;

namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Implementação do serviço de sincronização
/// Busca dados do Nomus e persiste localmente de forma idempotente
/// </summary>
public class SynchronizationService : ISynchronizationService
{
    private readonly INomusClient _nomusClient;
    private readonly IRepository<Boleto> _boletoRepository;
    private readonly IRepository<Recebimento> _recebimentoRepository;
    private readonly IRepository<ContaReceber> _contaReceberRepository;
    private readonly ILogger<SynchronizationService> _logger;

    public SynchronizationService(
        INomusClient nomusClient,
        IRepository<Boleto> boletoRepository,
        IRepository<Recebimento> recebimentoRepository,
        IRepository<ContaReceber> contaReceberRepository,
        ILogger<SynchronizationService> logger)
    {
        _nomusClient = nomusClient;
        _boletoRepository = boletoRepository;
        _recebimentoRepository = recebimentoRepository;
        _contaReceberRepository = contaReceberRepository;
        _logger = logger;
    }

    public async Task SynchronizeBoletosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando sincronização de boletos");

            var boletosDto = await _nomusClient.GetBoletosAsync(cancellationToken);
            var boletos = boletosDto.Select(BoletoMapper.ToDomain).ToList();

            _logger.LogInformation("Total de boletos recebidos: {Count}", boletos.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var boleto in boletos)
            {
                try
                {
                    await _boletoRepository.UpsertAsync(boleto, b => b.Id == boleto.Id, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, 
                        "Erro ao sincronizar boleto {BoletoId} (ContaReceber: {ContaReceberId}, Empresa: {EmpresaId})",
                        boleto.Id, boleto.IdContaReceber, boleto.IdEmpresa);
                }
            }

            _logger.LogInformation(
                "Sincronização de boletos concluída. Sucesso: {SuccessCount}, Erros: {ErrorCount}",
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar boletos");
            throw;
        }
    }

    public async Task SynchronizeRecebimentosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando sincronização de recebimentos");

            var recebimentosDto = await _nomusClient.GetRecebimentosAsync(cancellationToken);
            var recebimentos = recebimentosDto.Select(RecebimentoMapper.ToDomain).ToList();

            _logger.LogInformation("Total de recebimentos recebidos: {Count}", recebimentos.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var recebimento in recebimentos)
            {
                try
                {
                    await _recebimentoRepository.UpsertAsync(
                        recebimento, 
                        r => r.Id == recebimento.Id, 
                        cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Erro ao sincronizar recebimento {RecebimentoId} (ContaReceber: {ContaReceberId}, Empresa: {EmpresaId})",
                        recebimento.Id, recebimento.IdContaReceber, recebimento.IdEmpresa);
                }
            }

            _logger.LogInformation(
                "Sincronização de recebimentos concluída. Sucesso: {SuccessCount}, Erros: {ErrorCount}",
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar recebimentos");
            throw;
        }
    }

    public async Task SynchronizeContasReceberAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando sincronização de contas a receber");

            var contasDto = await _nomusClient.GetContasReceberAsync(cancellationToken);
            var contas = contasDto.Select(ContaReceberMapper.ToDomain).ToList();

            _logger.LogInformation("Total de contas a receber recebidas: {Count}", contas.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var conta in contas)
            {
                try
                {
                    await _contaReceberRepository.UpsertAsync(
                        conta, 
                        c => c.Id == conta.Id, 
                        cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Erro ao sincronizar conta a receber {ContaId} (Pessoa: {PessoaId}, Empresa: {EmpresaId})",
                        conta.Id, conta.IdPessoa, conta.IdEmpresa);
                }
            }

            _logger.LogInformation(
                "Sincronização de contas a receber concluída. Sucesso: {SuccessCount}, Erros: {ErrorCount}",
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar contas a receber");
            throw;
        }
    }

    public async Task SynchronizeAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando sincronização completa de todos os dados");

        var tasks = new[]
        {
            SynchronizeBoletosAsync(cancellationToken),
            SynchronizeRecebimentosAsync(cancellationToken),
            SynchronizeContasReceberAsync(cancellationToken)
        };

        await Task.WhenAll(tasks);

        _logger.LogInformation("Sincronização completa concluída");
    }
}

