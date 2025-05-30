using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Commands;

/// <summary>
/// Handler para comandos que não retornam dados específicos
/// </summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler para comandos que retornam dados específicos
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default);
}
