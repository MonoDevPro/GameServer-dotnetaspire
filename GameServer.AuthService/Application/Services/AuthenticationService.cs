using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Security;

namespace GameServer.AuthService.Application.Services;

/// <summary>
/// Implementação do serviço de autenticação
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validar dados
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse(false, string.Empty, "Todos os campos são obrigatórios.");
            }

            // Verificar se usuário já existe
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return new AuthResponse(false, string.Empty, "Nome de usuário já está em uso.");
            }

            // Verificar se email já existe
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return new AuthResponse(false, string.Empty, "E-mail já está em uso.");
            }

            // Criar hash da senha
            string passwordHash = _passwordHasher.HashPassword(request.Password);

            // Criar novo usuário
            var user = new User(request.Username, request.Email, passwordHash);
            await _userRepository.CreateAsync(user);

            // Gerar token
            string token = _tokenGenerator.GenerateToken(user);

            return new AuthResponse(true, token, "Usuário registrado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário: {Message}", ex.Message);
            return new AuthResponse(false, string.Empty, "Erro interno ao processar o registro.");
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // Validar dados
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse(false, string.Empty, "Nome de usuário/email e senha são obrigatórios.");
            }

            // Buscar usuário por nome de usuário ou email
            User? user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail);
            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(request.UsernameOrEmail);
            }

            // Verificar se o usuário existe
            if (user == null)
            {
                return new AuthResponse(false, string.Empty, "Credenciais inválidas.");
            }

            // Verificar se o usuário pode fazer login
            if (!user.CanLogin())
            {
                if (user.IsBanned)
                {
                    return new AuthResponse(
                        false, 
                        string.Empty, 
                        user.BannedUntil.HasValue 
                            ? $"Conta banida até {user.BannedUntil.Value.ToShortDateString()}. Motivo: {user.BanReason}"
                            : $"Conta banida permanentemente. Motivo: {user.BanReason}");
                }
                
                return new AuthResponse(false, string.Empty, "Conta desativada.");
            }

            // Verificar senha
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResponse(false, string.Empty, "Credenciais inválidas.");
            }

            // Atualizar último login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user);

            // Gerar token
            string token = _tokenGenerator.GenerateToken(user);

            return new AuthResponse(true, token, "Login realizado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar usuário: {Message}", ex.Message);
            return new AuthResponse(false, string.Empty, "Erro interno ao processar a autenticação.");
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        return Task.FromResult(_tokenGenerator.ValidateToken(token));
    }

    public Task<string?> GetTokenExpirationReasonAsync(string token)
    {
        return Task.FromResult(_tokenGenerator.GetExpirationReason(token));
    }
}