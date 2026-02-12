# Melhorias de Escalabilidade

## Problemas Identificados

### 1. **Processamento Sequencial de Clientes**
- **Problema**: Clientes eram processados um por vez em um loop sequencial
- **Impacto**: Com 100 clientes e ~3s por cliente, levaria ~5 minutos (no limite)
- **Risco**: Qualquer atraso causaria timeout do job

### 2. **Persistência Registro por Registro**
- **Problema**: Cada `SaveChangesAsync` era chamado individualmente
- **Impacto**: Múltiplas idas ao banco de dados (ex: 1000 registros = 1000 queries)
- **Risco**: Performance degradada e possível timeout

### 3. **Envio de Invoices sem Batching**
- **Problema**: Todas as invoices eram enviadas de uma vez
- **Impacto**: Payloads grandes podem causar timeout ou problemas de memória
- **Risco**: Falha total se houver problema com uma requisição grande

### 4. **Falta de Controle de Concorrência**
- **Problema**: Sem limite de requisições HTTP simultâneas
- **Impacto**: Pode sobrecarregar APIs externas (Nomus/Letmesee)
- **Risco**: Rate limiting, timeouts e falhas em cascata

## Soluções Implementadas

### 1. **Processamento Paralelo com SemaphoreSlim**
```csharp
// Processa até 10 clientes simultaneamente (configurável)
using var semaphore = new SemaphoreSlim(_options.MaxConcurrentCustomers);
var customerTasks = customersList.Select(customer => 
    ProcessCustomerWithSemaphoreAsync(semaphore, customer, cancellationToken));
```

**Benefícios**:
- Com 10 clientes em paralelo: 100 clientes / 10 = ~30 segundos (assumindo 3s por cliente)
- Reduz drasticamente o tempo total de processamento
- Controla recursos para não sobrecarregar APIs

### 2. **Bulk Operations para Persistência**
```csharp
// Processa em lotes de 500 registros (configurável)
await _dbContext.Boletos.BulkUpsertAsync(
    _dbContext, boletos, b => b.Id, _options.PersistenceBatchSize, cancellationToken);
```

**Benefícios**:
- 1000 registros = 2 SaveChangesAsync (ao invés de 1000)
- Reduz drasticamente round-trips ao banco
- Melhor uso do ChangeTracker do EF Core

### 3. **Batching de Invoices**
```csharp
// Envia invoices em batches de 100 (configurável)
for (int i = 0; i < invoices.Count; i += _options.InvoiceBatchSize)
{
    var batch = invoices.Skip(i).Take(_options.InvoiceBatchSize).ToList();
    await _letmeseeService.SendInvoicesAsync(batch, cancellationToken);
}
```

**Benefícios**:
- Payloads menores = menos chance de timeout
- Falha em um batch não impacta os outros
- Melhor controle de memória

### 4. **Configurações de Escalabilidade**
```json
{
  "Synchronization": {
    "MaxConcurrentCustomers": 10,          // Clientes processados em paralelo
    "InvoiceBatchSize": 100,                // Tamanho do batch de invoices
    "PersistenceBatchSize": 500,            // Tamanho do batch de persistência
    "CustomerProcessingTimeoutSeconds": 60  // Timeout por cliente
  }
}
```

**Benefícios**:
- Configurável por ambiente (dev/test/prod)
- Pode ser ajustado sem recompilação
- Permite fine-tuning baseado em métricas

### 5. **Timeout por Cliente**
```csharp
// Timeout individual para cada cliente
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(TimeSpan.FromSeconds(_options.CustomerProcessingTimeoutSeconds));
```

**Benefícios**:
- Um cliente lento não trava todo o processamento
- Garante progresso mesmo com problemas pontuais
- Logs específicos para timeouts

## Métricas de Performance

### Antes (Sequencial)
- 100 clientes × 3s = **~5 minutos** (no limite)
- 1000 registros × 50ms SaveChanges = **~50 segundos** só de persistência
- **Risco alto de timeout**

### Depois (Paralelo + Bulk)
- 100 clientes ÷ 10 paralelos × 3s = **~30 segundos**
- 1000 registros ÷ 500 batch = **~1 segundo** de persistência
- **Tempo total: ~1-2 minutos** (bem dentro do limite de 5 minutos)

## Recomendações

### Para Produção

1. **Ajustar MaxConcurrentCustomers**:
   - Começar com 10 e monitorar
   - Se APIs externas suportarem, aumentar para 15-20
   - Considerar limite de rate limiting das APIs

2. **Monitorar Métricas**:
   - Tempo médio por cliente
   - Taxa de sucesso/falha
   - Tempo total de execução
   - Uso de memória

3. **Considerar Filtros Adicionais**:
   - Processar apenas clientes atualizados recentemente
   - Priorizar clientes críticos
   - Implementar cache de clientes não alterados

4. **Escalabilidade Horizontal**:
   - Se necessário, considerar múltiplas instâncias do Hangfire
   - Usar filas separadas por prioridade
   - Implementar load balancing

### Monitoramento

Logs estruturados incluem:
- `CorrelationId`: Para rastrear execução completa
- `UserGroupId`: Para rastrear cliente específico
- Duração por cliente e total
- Contadores de sucesso/falha

## Próximos Passos Sugeridos

1. **Implementar métricas Prometheus/Application Insights**
2. **Adicionar circuit breaker específico por cliente**
3. **Implementar retry policy com backoff exponencial**
4. **Considerar processamento incremental (apenas mudanças)**
5. **Adicionar health checks para APIs externas**

