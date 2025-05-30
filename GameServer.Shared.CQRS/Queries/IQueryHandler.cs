using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Queries;

/// <summary>
/// Handler para queries
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
