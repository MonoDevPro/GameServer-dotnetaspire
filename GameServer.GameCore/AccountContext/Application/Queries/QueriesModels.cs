using GameServer.GameCore.Shared.CQRS.Queries;

namespace GameServer.GameCore.AccountContext.Application.Queries;

public class QueriesModels
{
    public record GetAccountQuery(string Email) : IQuery<AccountDto>;
    public record AccountDto(string Email, string Name, DateTime CreatedAt);
}