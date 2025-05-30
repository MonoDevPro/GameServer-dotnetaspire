namespace GameServer.Shared.CQRS.Queries;

/// <summary>
/// Interface base para queries que sempre retornam dados
/// </summary>
/// <typeparam name="TResult">Tipo do resultado da query</typeparam>
public interface IQuery<TResult>
{
}
