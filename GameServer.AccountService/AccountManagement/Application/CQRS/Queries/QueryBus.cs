using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Results;
using GameServer.AccountService.AccountManagement.Ports.In;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries;

/// <summary>
/// Implementação padrão do barramento de queries CQRS.
/// </summary>
public class QueryBus : IQueryBus
{
    private readonly IServiceProvider _serviceProvider;

    public QueryBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ResultQuery<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler == null)
            throw new InvalidOperationException($"Handler for query type {typeof(TQuery).Name} not registered.");

        return await handler.HandleAsync(query, cancellationToken);
    }
}
