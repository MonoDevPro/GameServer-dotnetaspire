using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.Shared.CQRS.Queries;
using GameServer.GameCore.Shared.CQRS.Queries.Repository;
using GameServer.GameCore.Shared.CQRS.Results;

namespace GameServer.GameCore.AccountContext.Application.Queries.Handlers;

public class GetAccountQueryHandler : IQueryHandler<QueriesModels.GetAccountQuery, QueriesModels.AccountDto>
{
    private readonly IReaderRepository<Account> _repository;

    public GetAccountQueryHandler(IReaderRepository<Account> repository)
    {
        _repository = repository;
    }

    public async Task<Result> HandleAsync(QueriesModels.GetAccountQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _repository.QuerySingleAsync(
                filter: a => a.Email == query.Email,
                selector: a => new QueriesModels.AccountDto(a.Email, a.Name, a.CreatedAt),
                cancellationToken);

            return account is not null 
                ? Result.Success(account)
                : Result.Failure("Account not found");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error retrieving account: {ex.Message}");
        }
    }
}