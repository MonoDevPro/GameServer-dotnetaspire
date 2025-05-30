using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Queries;

/// <summary>
/// Interface para o barramento de queries
/// </summary>
public interface IQueryBus
{
    /// <summary>
    /// Envia uma query que retorna dados específicos
    /// </summary>
    Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}
