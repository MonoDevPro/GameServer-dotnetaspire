using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Infrastructure.Adapters.Out.Security;

namespace GameServer.AuthService.Application.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de contas
/// </summary>
public class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        ILogger<AccountService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountResponse?> GetAccountDetailsAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            return new AccountResponse(
                user.Id,
                user.Username,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.IsBanned,
                user.BannedUntil);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar detalhes da conta do usuário {UserId}: {Message}", userId, ex.Message);
            return null;
        }
    }

    public async Task<AccountResponse?> UpdateAccountAsync(Guid userId, UpdateAccountRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            // Verifica se há atualizações para fazer
            bool hasUpdates = false;

            // Atualiza o email se fornecido
            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                // Verifica se o novo email já está em uso
                if (await _userRepository.EmailExistsAsync(request.Email))
                {
                    throw new InvalidOperationException("O e-mail já está em uso.");
                }

                user.UpdateEmail(request.Email);
                hasUpdates = true;
            }

            // Atualiza a senha se fornecida, verificando a senha atual
            if (!string.IsNullOrEmpty(request.CurrentPassword) && !string.IsNullOrEmpty(request.NewPassword))
            {
                if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    // Lançamos uma exceção específica para senha incorreta
                    throw new InvalidOperationException("Senha atual incorreta.");
                }

                string newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                user.UpdatePassword(newPasswordHash);
                hasUpdates = true;
            }

            // Se houver atualizações, salva as alterações
            if (hasUpdates)
            {
                await _userRepository.UpdateAsync(user);
            }

            return new AccountResponse(
                user.Id,
                user.Username,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.IsBanned,
                user.BannedUntil);
        }
        catch (InvalidOperationException ex)
        {
            // Registramos o erro, mas mantemos a mensagem original para ser tratada pelo controlador
            _logger.LogWarning("Operação inválida ao atualizar conta do usuário {UserId}: {Message}", userId, ex.Message);
            throw; // Propaga a exceção original
        }
        catch (Exception ex)
        {
            // Para outros tipos de exceção, podemos logar e lançar uma mensagem mais específica
            _logger.LogError(ex, "Erro ao atualizar conta do usuário {UserId}: {Message}", userId, ex.Message);
            
            // Decidimos lançar a exceção original em vez de encapsulá-la em uma mensagem genérica
            // para preservar o contexto do erro e facilitar os testes
            throw;
        }
    }

    public async Task<bool> BanAccountAsync(Guid userId, DateTime until, string reason)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Ban(until, reason);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao banir usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> UnbanAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Unban();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desbanir usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> DeactivateAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Deactivate();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar conta do usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ActivateAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Activate();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar conta do usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> PromoteToAdminAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Tentativa de promover usuário não encontrado: {UserId}", userId);
                return false;
            }

            // Se já for admin, não é necessário fazer nada
            if (user.IsAdmin)
            {
                _logger.LogInformation("Usuário {UserId} já é administrador", userId);
                return true;
            }

            user.PromoteToAdmin();
            _logger.LogInformation("Usuário {UserId} promovido a administrador", userId);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao promover usuário {UserId} a administrador: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> DemoteFromAdminAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Tentativa de remover privilégios de admin de usuário não encontrado: {UserId}", userId);
                return false;
            }

            // Se não for admin, não é necessário fazer nada
            if (!user.IsAdmin)
            {
                _logger.LogInformation("Usuário {UserId} não é administrador", userId);
                return true;
            }

            user.DemoteFromAdmin();
            _logger.LogInformation("Privilégios de administrador removidos do usuário {UserId}", userId);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover privilégios de administrador do usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> IsAdminAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Tentativa de verificar privilégios de admin de usuário não encontrado: {UserId}", userId);
                return false;
            }

            return user.IsAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar privilégios de administrador do usuário {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }
}

