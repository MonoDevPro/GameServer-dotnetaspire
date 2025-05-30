using System.Linq.Expressions;
using GameServer.Shared.Database.Repository.UnityOfWork;
using GameServer.Shared.PagedListCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GameServer.Shared.Database.Repository.Reader;

public interface IReaderRepository<TEntity>
    where TEntity : class
{
    // Métodos otimizados para contagem e verificação de existência
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter, 
        CancellationToken cancellationToken = default);
    Task<long> CountAsync(
        Expression<Func<TEntity, bool>> filter, 
        CancellationToken cancellationToken = default);
    
    // Adicionar métodos com projeção para outros tipos
    Task<List<TResult>> QueryListAsync<TResult>(
        Expression<Func<TEntity, bool>> filter,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    Task<TResult?> QuerySingleAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        TrackingType trackingType = TrackingType.NoTracking,
        CancellationToken cancellationToken = default);
    
    // Novos métodos para paginação
    Task<IPagedList<TResult>> QueryPagedListAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        int pageIndex = 0,
        int pageSize = 10,
        TrackingType trackingType = TrackingType.NoTracking,
        CancellationToken cancellationToken = default,
        bool ignoreQueryFilters = false,
        bool ignoreAutoIncludes = false);
}