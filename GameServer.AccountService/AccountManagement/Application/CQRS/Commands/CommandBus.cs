using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Ports.In;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands;

/// <summary>
/// Implementação padrão do barramento de comandos CQRS.
/// </summary>
public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;

    public CommandBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
        where TResult : ResultCommand
    {
        var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler == null)
            throw new InvalidOperationException($"Handler for command type {typeof(TCommand).Name} not registered.");

        return await handler.HandleAsync(command, cancellationToken);
    }
}