using Microsoft.EntityFrameworkCore;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;

/// <summary>
/// Operações de bulk para melhorar performance de inserção/atualização
/// </summary>
public static class BulkOperations
{
    /// <summary>
    /// Realiza upsert em lote de forma eficiente usando chave primária (Id)
    /// </summary>
    public static async Task BulkUpsertAsync<T>(
        this DbSet<T> dbSet,
        ApplicationDbContext context,
        IEnumerable<T> entities,
        Func<T, int> keySelector,
        int batchSize = 500,
        CancellationToken cancellationToken = default) where T : class
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        // Processar em lotes para evitar problemas de memória
        for (int i = 0; i < entitiesList.Count; i += batchSize)
        {
            var batch = entitiesList.Skip(i).Take(batchSize).ToList();
            var keys = batch.Select(keySelector).ToList();

            // Buscar entidades existentes
            // Nota: Para melhor performance em grandes volumes, considere usar uma biblioteca como
            // Z.EntityFramework.Extensions ou EFCore.BulkExtensions que suporta bulk operations nativas
            var existingEntitiesDict = new Dictionary<int, T>();
            
            // Usar FindAsync sequencialmente (mais seguro para DbContext compartilhado)
            // Alternativa seria usar uma query com IN clause, mas requer reflection/expressions
            foreach (var key in keys)
            {
                var existing = await dbSet.FindAsync(new object[] { key }, cancellationToken);
                if (existing != null)
                {
                    existingEntitiesDict[key] = existing;
                }
            }

            // Adicionar ou atualizar entidades
            foreach (var entity in batch)
            {
                var key = keySelector(entity);
                if (existingEntitiesDict.TryGetValue(key, out var existing))
                {
                    context.Entry(existing).CurrentValues.SetValues(entity);
                }
                else
                {
                    await dbSet.AddAsync(entity, cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            
            // Limpar o ChangeTracker para liberar memória
            context.ChangeTracker.Clear();
        }
    }
}

