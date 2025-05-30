using System.Linq.Expressions;
using GameServer.Shared.Database.Repository.UnityOfWork;
using GameServer.Shared.PagedListCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GameServer.Shared.Database.Repository.Reader;

public class ReaderRepository<TEntity> : IReaderRepository<TEntity>
    where TEntity : class
{
    private readonly IQueryable<TEntity> _context;
    public ReaderRepository(IQueryable<TEntity> context) => _context = context;

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AnyAsync(filter, cancellationToken);
    }

    public async Task<long> CountAsync(
        Expression<Func<TEntity, bool>> filter, 
        CancellationToken cancellationToken = default)
    {
        return await _context.CountAsync(filter, cancellationToken);
    }

    public async Task<List<TResult>> QueryListAsync<TResult>(
        Expression<Func<TEntity, bool>> filter,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await _context
            .Where(filter)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<TResult?> QuerySingleAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        TrackingType trackingType = TrackingType.NoTracking,
        CancellationToken cancellationToken = default)
    {
        // 1. Definição do IQueryable base conforme tipo de rastreamento
        IQueryable<TEntity> query = trackingType switch
        {
            TrackingType.NoTracking => _context.AsNoTracking(),
            TrackingType.NoTrackingWithIdentityResolution => _context.AsNoTrackingWithIdentityResolution(),
            TrackingType.Tracking => _context.AsTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(trackingType), trackingType, null)
        };

        // 2. Include de navegações
        if (include is not null)
            query = include(query);

        // 3. Filtro Where
        if (predicate is not null)
            query = query.Where(predicate);

        // 6. Projeção (Select) e paginação
        if (selector is not null)
        {
            var projected = query.Select(selector);
            return await projected.SingleOrDefaultAsync(cancellationToken);
        }
        else
        {
            // Sem projeção, retorna entidade diretamente
            return await query.Cast<TResult>().SingleOrDefaultAsync(cancellationToken);
        }
    }
    
    public async Task<IPagedList<TResult>> QueryPagedListAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        int pageIndex = 0,
        int pageSize = 20,
        TrackingType trackingType = TrackingType.NoTracking,
        CancellationToken cancellationToken = default,
        bool ignoreQueryFilters = false,
        bool ignoreAutoIncludes = false)
    {
        // 1. Definição do IQueryable base conforme tipo de rastreamento
        IQueryable<TEntity> query = trackingType switch
        {
            TrackingType.NoTracking => _context.AsNoTracking(),
            TrackingType.NoTrackingWithIdentityResolution => _context.AsNoTrackingWithIdentityResolution(),
            TrackingType.Tracking => _context.AsTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(trackingType), trackingType, null)
        };

        // 2. Include de navegações
        if (include is not null)
            query = include(query);

        // 3. Filtro Where
        if (predicate is not null)
            query = query.Where(predicate);

        // 4. Opções de filtros globais e auto-includes
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();
        if (ignoreAutoIncludes)
            query = query.IgnoreAutoIncludes();

        // 5. Ordenação com ascending/descending
        if (orderBy is not null)
        {
            query = orderByDescending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }

        // 6. Projeção (Select) e paginação
        if (selector is not null)
        {
            var projected = query.Select(selector);
            return await projected.ToPagedListAsync(pageIndex, pageSize, cancellationToken: cancellationToken);
        }
        else
        {
            // Sem projeção, retorna entidade diretamente
            return await query.Cast<TResult>().ToPagedListAsync(pageIndex, pageSize, cancellationToken: cancellationToken);
        }
    }
}