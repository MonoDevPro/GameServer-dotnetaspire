using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Implementação do gerador de tokens com suporte a cache
/// </summary>
public class CachedTokenGenerator : ITokenGenerator
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserCache _cache;
    private readonly ILogger<CachedTokenGenerator> _logger;

    public CachedTokenGenerator(
        ITokenGenerator tokenGenerator,
        IUserCache cache,
        ILogger<CachedTokenGenerator> logger)
    {
        _tokenGenerator = tokenGenerator;
        _cache = cache;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        // Gera o token usando o gerador original
        var token = _tokenGenerator.GenerateToken(user);
        
        // Extrai os claims do token para obter a data de expiração
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiration = jwtToken.ValidTo;
        
        // Armazena o token no cache
        _cache.AddValidTokenAsync(token, user.Id, expiration).ConfigureAwait(false);
        _logger.LogDebug("Token gerado e armazenado no cache para usuário {UserId}", user.Id);
        
        return token;
    }

    public bool ValidateToken(string token)
    {
        // Primeiro verifica no cache para evitar validação criptográfica
        if (_cache.IsTokenValidAsync(token).Result)
        {
            _logger.LogDebug("Token validado pelo cache");
            return true;
        }
        
        // Se não estiver no cache ou for inválido, valida usando o gerador original
        var isValid = _tokenGenerator.ValidateToken(token);
        
        // Se for válido, armazena no cache para futuras validações
        if (isValid)
        {
            var userId = _tokenGenerator.GetUserIdFromToken(token);
            if (userId.HasValue)
            {
                // Usa o JWT Handler para extrair a data de expiração
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expiration = jwtToken.ValidTo;
                
                _cache.AddValidTokenAsync(token, userId.Value, expiration).ConfigureAwait(false);
                _logger.LogDebug("Token validado e adicionado ao cache");
            }
        }
        
        return isValid;
    }

    public Guid? GetUserIdFromToken(string token)
    {
        return _tokenGenerator.GetUserIdFromToken(token);
    }

    public string GetExpirationReason(string token)
    {
        // Delega a verificação do motivo de expiração para o gerador de token subjacente
        return _tokenGenerator.GetExpirationReason(token) ?? string.Empty;
    }
}