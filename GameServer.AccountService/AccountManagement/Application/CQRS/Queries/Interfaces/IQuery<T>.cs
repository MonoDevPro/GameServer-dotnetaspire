namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;

public interface IQuery<T>
{
    Guid QueryId { get; }
    DateTime Timestamp { get; }
}