using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Domain.Events;
using GameServer.GameCore.AccountContext.Domain.Exceptions;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;
using GameServer.GameCore.AccountContext.Ports.Out.Messaging;
using GameServer.GameCore.AccountContext.Ports.Out.Persistence;
using GameServer.GameCore.AccountContext.Ports.Out.Security;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Results;

namespace GameServer.GameCore.AccountContext.Application.Commands;

public class AccountCommandHandlers
{
    public class CreateAccountCommandHandler : ICommandHandler<AccountCommands.RegisterCommand>
    {
        private readonly IAccountRepositoryReader _accountRepositoryReader;
        private readonly IAccountRepositoryWriter _accountRepositoryWriter;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ILogger<CreateAccountCommandHandler> _logger;

        public CreateAccountCommandHandler(
            IAccountRepositoryReader accountRepositoryReader,
            IAccountRepositoryWriter accountRepositoryWriter,
            IPasswordHashService passwordHashService,
            ILogger<CreateAccountCommandHandler> logger
        )
        {
            _accountRepositoryReader = accountRepositoryReader;
            _accountRepositoryWriter = accountRepositoryWriter;
            _passwordHashService = passwordHashService;
            _logger = logger;
        }

        public async Task<Result> HandleAsync(AccountCommands.RegisterCommand command, CancellationToken cancellationToken = default)
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
                await _accountRepositoryWriter.SaveAsync(account);

                // Retornar o resultado
                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar conta");
                return Result.Failure("Erro interno ao processar o registro");
            }
        }
    }
    
    public class ChangePasswordCommandHandler : ICommandHandler<AccountCommands.ChangePasswordCommand>
    {
        private readonly IAccountRepositoryWriter _accountRepositoryWriter;
        private readonly IAccountRepositoryReader _accountRepositoryReader;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IAccountEventBus _eventPublisher;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            IAccountRepositoryWriter accountRepositoryWriter,
            IAccountRepositoryReader accountRepositoryReader,
            IPasswordHashService passwordHasher,
            IAccountEventBus eventPublisher,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _accountRepositoryWriter = accountRepositoryWriter;
            _accountRepositoryReader = accountRepositoryReader;
            _passwordHasher = passwordHasher;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<Result> HandleAsync(AccountCommands.ChangePasswordCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var account = await _accountRepositoryReader.GetByIdAsync(request.AccountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found for password change", request.AccountId);
                    return Result.Failure("Conta não encontrada");
                }

                // Verificar senha atual
                if (!account.Password.Verify(request.CurrentPassword, _passwordHasher))
                {
                    _logger.LogWarning("Invalid current password provided for account {AccountId}", request.AccountId);
                    return Result.Failure("Senha atual incorreta");
                }

                // Criar nova senha
                var newPasswordVO = PasswordVO.Create(request.NewPassword, _passwordHasher);

                // Atualizar senha da conta
                account.UpdatePassword(newPasswordVO);

                // Salvar alterações
                var success = await _accountRepositoryWriter.SaveAsync(account);
                if (!success)
                    return Result.Failure("Erro ao salvar alterações");

                // Publicar evento
                var passwordUpdatedEvent = new AccountPasswordUpdated(account.Id);
                await _eventPublisher.PublishAsync(passwordUpdatedEvent);

                _logger.LogInformation("Password changed successfully for account {AccountId} from IP {IpAddress}",
                    request.AccountId, request.IpAddress ?? "unknown");

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for account {AccountId}", request.AccountId);
                return Result.Failure("Erro interno ao alterar senha");
            }
        }
    }
}