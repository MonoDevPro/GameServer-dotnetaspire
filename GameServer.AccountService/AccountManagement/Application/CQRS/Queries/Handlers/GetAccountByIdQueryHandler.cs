using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Models;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Results;
using GameServer.AccountService.AccountManagement.Application.DTO;
using GameServer.AccountService.AccountManagement.Ports.Out.Persistence;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Handlers;

public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<GetAccountByIdQueryHandler> _logger;

    public GetAccountByIdQueryHandler(
        IAccountRepository accountRepository, 
        ILogger<GetAccountByIdQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<ResultQuery<AccountDto>> HandleAsync(
        GetAccountByIdQuery query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(query.Id);
            
            if (account == null)
                return ResultQuery<AccountDto>.Failure("Conta não encontrada");

            var dto = new AccountDto(
                account.Id,
                account.UniqueId,
                account.Username.Value,
                account.Email.Value,
                null, // FirstName não disponível na entidade Account
                null  // LastName não disponível na entidade Account
            );
            
            return ResultQuery<AccountDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar conta por ID");
            return ResultQuery<AccountDto>.Failure("Erro interno ao processar a consulta");
        }
    }
}