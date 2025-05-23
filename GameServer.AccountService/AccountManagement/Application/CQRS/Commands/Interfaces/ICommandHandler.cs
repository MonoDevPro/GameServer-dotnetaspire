using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;

/// <summary>
/// Interface para manipuladores de comandos CQRS
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : ResultCommand
{
    /// <summary>
    /// Manipula o comando de forma assíncrona
    /// </summary>
    /// <param name="command">Comando a ser processado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do comando encapsulado em Result</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}