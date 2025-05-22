using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;

namespace GameServer.AccountService.AccountManagement.Ports.In;

/// <summary>
/// Interface para o barramento de comandos CQRS
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Envia um comando que produz resultado para processamento pelo manipulador apropriado
    /// </summary>
    /// <typeparam name="TCommand">Tipo do comando</typeparam>
    /// <typeparam name="TResult">Tipo do resultado esperado</typeparam>
    /// <param name="command">Comando a ser processado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do comando encapsulado em Result</returns>
    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult> 
        where TResult : ResultCommand;
}