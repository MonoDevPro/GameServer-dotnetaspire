using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior para interceptar e processar queries antes/depois da execução
/// </summary>
public interface IQueryBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, Func<TQuery, Task<Result<TResult>>> next, CancellationToken cancellationToken = default);
}
