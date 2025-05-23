using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Models;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.Exceptions;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;
using GameServer.AccountService.AccountManagement.Ports.Out.Identity;
using GameServer.AccountService.AccountManagement.Ports.Out.Persistence;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Handlers;

public class CreateAccountCommandHandler : ICommandHandler<RegisterCommand, ResultCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IAccountIdentitySyncService _identitySyncService;
    private readonly ILogger<CreateAccountCommandHandler> _logger;
    
    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashService passwordHashService,
        IAccountIdentitySyncService identitySyncService,
        ILogger<CreateAccountCommandHandler> logger
        )
    {
        _accountRepository = accountRepository;
        _passwordHashService = passwordHashService;
        _identitySyncService = identitySyncService;
        _logger = logger;
    }
    
    public async Task<ResultCommand> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Lógica de validação
            // Criar objetos de valor
            var username = UsernameVO.Create(command.Username);
            var email = EmailVO.Create(command.Email);
            var password = PasswordVO.Create(command.Password, _passwordHashService);
            
            // Criar entidade de domínio
            var account = new Account(username, email, password);
            
            // Salvar
            await _accountRepository.SaveAsync(account);
            
            // Sincronizar com Identity
            await _identitySyncService.SyncToIdentityAsync(account);
            
            // Retornar o resultado
            return ResultCommand.Success();
        }
        catch (DomainException ex)
        {
            return ResultCommand.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar conta");
            return ResultCommand.Failure("Erro interno ao processar o registro");
        }
    }
}