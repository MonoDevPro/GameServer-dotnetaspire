using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;

public interface ICommand
{
}

/// <summary>
/// Interface para comandos CQRS
/// </summary>
/// <typeparam name="TResult">Tipo do resultado do comando</typeparam>
public interface ICommand<TResult> : ICommand
    where TResult : ResultCommand
{
    
}