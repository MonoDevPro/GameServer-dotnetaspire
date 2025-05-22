using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Results;

namespace GameServer.AccountService.AccountManagement.Ports.In;

/// <summary>
/// Interface para o barramento de consultas CQRS
/// </summary>
public interface IQueryBus
{
    /// <summary>
    /// Envia uma consulta para processamento pelo manipulador apropriado
    /// </summary>
    /// <typeparam name="TQuery">Tipo da consulta</typeparam>
    /// <typeparam name="TResult">Tipo do resultado esperado</typeparam>
    /// <param name="query">Consulta a ser processada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da consulta encapsulado em Result</returns>
    Task<ResultQuery<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}