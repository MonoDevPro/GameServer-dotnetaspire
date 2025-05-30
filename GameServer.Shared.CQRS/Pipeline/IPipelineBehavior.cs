using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Pipeline;

/// <summary>
/// Delegate para representar o próximo handler na pipeline
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Interface base para behaviors que interceptam requests/responses na pipeline
/// </summary>
/// <typeparam name="TRequest">Tipo do request</typeparam>
/// <typeparam name="TResponse">Tipo do response</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Processa o request e chama o próximo behavior ou handler na pipeline
    /// </summary>
    /// <param name="request">O request sendo processado</param>
    /// <param name="next">Delegate para chamar o próximo behavior/handler</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response do handler</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Interface para behaviors específicos de comandos
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
public interface ICommandPipelineBehavior<in TCommand> : IPipelineBehavior<TCommand, Result>
{
}

/// <summary>
/// Interface para behaviors específicos de comandos com resultado
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public interface ICommandPipelineBehavior<in TCommand, TResult> : IPipelineBehavior<TCommand, Result<TResult>>
{
}

/// <summary>
/// Interface para behaviors específicos de queries
/// </summary>
/// <typeparam name="TQuery">Tipo da query</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public interface IQueryPipelineBehavior<in TQuery, TResult> : IPipelineBehavior<TQuery, Result<TResult>>
{
}
