using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Results;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Manipula a consulta de forma assíncrona
    /// </summary>
    /// <param name="query">Consulta a ser processada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da consulta encapsulado em Result</returns>
    Task<ResultQuery<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}