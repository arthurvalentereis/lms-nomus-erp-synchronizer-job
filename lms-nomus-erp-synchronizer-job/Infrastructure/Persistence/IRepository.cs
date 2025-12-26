namespace lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;

/// <summary>
/// Interface genérica para repositório
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Insere ou atualiza uma entidade baseado em uma condição
    /// </summary>
    Task UpsertAsync(T entity, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca uma entidade por ID
    /// </summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
}

