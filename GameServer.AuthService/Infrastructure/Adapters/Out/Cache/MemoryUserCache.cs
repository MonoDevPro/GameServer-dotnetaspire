using System.Text.Json;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Opções de configuração para o cache de usuários
/// </summary>
public class UserCacheOptions
{
    public const string SectionName = "UserCache";
    
    /// <summary>
    /// Tempo de expiração do cache de usuários em minutos
    /// </summary>
    public int UserCacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Limite máximo de entradas no cache
    /// </summary>
    public int SizeLimit { get; set; } = 1000;
}

/// <summary>
/// Implementação do serviço de cache de usuários usando MemoryCache
/// </summary>
public class MemoryUserCache : IUserCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryUserCache> _logger;
    private readonly MemoryCacheEntryOptions _userCacheOptions;
    private readonly MemoryCacheEntryOptions _tokenCacheOptions;

    // Prefixos para as chaves do cache para evitar colisões
    private const string UserByIdPrefix = "user:id:";
    private const string UserByUsernamePrefix = "user:username:";
    private const string UserByEmailPrefix = "user:email:";
    private const string TokenPrefix = "token:";
    
    public MemoryUserCache(
        IMemoryCache cache, 
        ILogger<MemoryUserCache> logger, 
        IOptions<UserCacheOptions> options)
    {
        _cache = cache;
        _logger = logger;
        
        // Configuração do cache de usuários
        _userCacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(options.Value.UserCacheExpirationMinutes))
            .SetSize(1) // 1 unidade de tamanho por entrada
            .SetPriority(CacheItemPriority.Normal);

        // Configuração do cache de tokens
        _tokenCacheOptions = new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High) // Tokens têm alta prioridade
            .SetSize(1);
    }

    public Task<User?> GetUserByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{UserByIdPrefix}{id}";
            if (_cache.TryGetValue(cacheKey, out User? user))
            {
                _logger.LogDebug("Usuário {UserId} encontrado no cache", id);
                return Task.FromResult(user);
            }

            _logger.LogDebug("Usuário {UserId} não encontrado no cache", id);
            return Task.FromResult<User?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar usuário {UserId} no cache", id);
            return Task.FromResult<User?>(null);
        }
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return Task.FromResult<User?>(null);
        
        try
        {
            var cacheKey = $"{UserByUsernamePrefix}{username.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out Guid userId))
            {
                // Se encontrarmos o ID no cache, buscamos o usuário pelo ID
                return GetUserByIdAsync(userId);
            }

            _logger.LogDebug("Usuário com username {Username} não encontrado no cache", username);
            return Task.FromResult<User?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar usuário pelo username {Username} no cache", username);
            return Task.FromResult<User?>(null);
        }
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<User?>(null);
        
        try
        {
            var cacheKey = $"{UserByEmailPrefix}{email.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out Guid userId))
            {
                // Se encontrarmos o ID no cache, buscamos o usuário pelo ID
                return GetUserByIdAsync(userId);
            }

            _logger.LogDebug("Usuário com email {Email} não encontrado no cache", email);
            return Task.FromResult<User?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar usuário pelo email {Email} no cache", email);
            return Task.FromResult<User?>(null);
        }
    }

    public Task SetUserAsync(User user)
    {
        if (user == null) return Task.CompletedTask;
        
        try
        {
            // Armazenamos o usuário completo usando o ID como chave
            var userIdKey = $"{UserByIdPrefix}{user.Id}";
            _cache.Set(userIdKey, user, _userCacheOptions);

            // Também armazenamos índices para username e email
            var usernameKey = $"{UserByUsernamePrefix}{user.Username.ToLower()}";
            var emailKey = $"{UserByEmailPrefix}{user.Email.ToLower()}";
            
            _cache.Set(usernameKey, user.Id, _userCacheOptions);
            _cache.Set(emailKey, user.Id, _userCacheOptions);

            _logger.LogDebug("Usuário {UserId} armazenado no cache", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao armazenar usuário {UserId} no cache", user.Id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(Guid id)
    {
        try
        {
            // Primeiro obtemos o usuário para remover também as referências de username e email
            if (_cache.TryGetValue($"{UserByIdPrefix}{id}", out User? user) && user != null)
            {
                _cache.Remove($"{UserByUsernamePrefix}{user.Username.ToLower()}");
                _cache.Remove($"{UserByEmailPrefix}{user.Email.ToLower()}");
            }
            
            _cache.Remove($"{UserByIdPrefix}{id}");
            _logger.LogDebug("Usuário {UserId} removido do cache", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover usuário {UserId} do cache", id);
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.FromResult(false);
        
        try
        {
            var cacheKey = $"{TokenPrefix}{token.GetHashCode()}";
            return Task.FromResult(_cache.TryGetValue(cacheKey, out _));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao verificar validade do token no cache");
            return Task.FromResult(false);
        }
    }

    public Task AddValidTokenAsync(string token, Guid userId, DateTime expiration)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.CompletedTask;
        
        try
        {
            var cacheKey = $"{TokenPrefix}{token.GetHashCode()}";
            
            // Definimos a expiração absoluta para a data de expiração do token
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .SetPriority(CacheItemPriority.High)
                .SetSize(1);
            
            // Armazenamos o userId associado ao token
            _cache.Set(cacheKey, userId, options);
            _logger.LogDebug("Token para usuário {UserId} armazenado no cache até {Expiration}", userId, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao armazenar token no cache");
        }

        return Task.CompletedTask;
    }

    public Task InvalidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.CompletedTask;
        
        try
        {
            var cacheKey = $"{TokenPrefix}{token.GetHashCode()}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Token invalidado no cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao invalidar token no cache");
        }

        return Task.CompletedTask;
    }
}