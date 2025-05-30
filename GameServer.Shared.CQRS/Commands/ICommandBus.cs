using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Commands;

/// <summary>
/// Interface unificada para o barramento de comandos
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Envia um comando que não retorna dados específicos
    /// </summary>
    Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Envia um comando que retorna dados específicos
    /// </summary>
    Task<Result<TResult>> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
