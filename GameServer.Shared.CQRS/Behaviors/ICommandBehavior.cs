using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior para interceptar e processar comandos antes/depois da execução
/// </summary>
public interface ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, Func<TCommand, Task<Result>> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Behavior para interceptar e processar comandos com resultado antes/depois da execução
/// </summary>
public interface ICommandBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, Func<TCommand, Task<Result<TResult>>> next, CancellationToken cancellationToken = default);
}
