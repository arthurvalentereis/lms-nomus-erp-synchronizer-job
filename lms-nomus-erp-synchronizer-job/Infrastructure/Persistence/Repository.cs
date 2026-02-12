using Microsoft.EntityFrameworkCore;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;

/// <summary>
/// Implementação genérica de repositório usando Entity Framework Core
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task UpsertAsync(T entity, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        
        if (existing == null)
        {
            await DbSet.AddAsync(entity, cancellationToken);
        }
        else
        {
            Context.Entry(existing).CurrentValues.SetValues(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task BulkUpsertAsync(IEnumerable<T> entities, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        // Compilar a expressão para melhor performance
        var compiledPredicate = predicate.Compile();

        // Buscar entidades existentes (adaptar conforme necessário - pode precisar de chave primária)
        // Esta é uma implementação simplificada - pode ser otimizada ainda mais
        var existingEntities = await DbSet.ToListAsync(cancellationToken);
        
        foreach (var entity in entitiesList)
        {
            var existing = existingEntities.FirstOrDefault(compiledPredicate);
            if (existing == null)
            {
                await DbSet.AddAsync(entity, cancellationToken);
            }
            else
            {
                Context.Entry(existing).CurrentValues.SetValues(entity);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new[] { id }, cancellationToken);
    }
}
