using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Implementação de repositório de usuários que utiliza cache
/// </summary>
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _repository;
    private readonly IUserCache _cache;
    private readonly ILogger<CachedUserRepository> _logger;

    public CachedUserRepository(
        IUserRepository repository,
        IUserCache cache,
        ILogger<CachedUserRepository> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        // Tenta obter do cache primeiro
        var user = await _cache.GetUserByIdAsync(id);
        if (user != null)
        {
            _logger.LogDebug("Usuário {UserId} obtido do cache", id);
            return user;
        }

        // Se não estiver no cache, busca do repositório
        user = await _repository.GetByIdAsync(id);

        // Se encontrado, armazena no cache para consultas futuras
        if (user != null)
        {
            await _cache.SetUserAsync(user);
            _logger.LogDebug("Usuário {UserId} armazenado no cache após busca no repositório", id);
        }

        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        // Tenta obter do cache primeiro
        var user = await _cache.GetUserByUsernameAsync(username);
        if (user != null)
        {
            _logger.LogDebug("Usuário com username {Username} obtido do cache", username);
            return user;
        }

        // Se não estiver no cache, busca do repositório
        user = await _repository.GetByUsernameAsync(username);

        // Se encontrado, armazena no cache para consultas futuras
        if (user != null)
        {
            await _cache.SetUserAsync(user);
            _logger.LogDebug("Usuário com username {Username} armazenado no cache após busca no repositório", username);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // Tenta obter do cache primeiro
        var user = await _cache.GetUserByEmailAsync(email);
        if (user != null)
        {
            _logger.LogDebug("Usuário com email {Email} obtido do cache", email);
            return user;
        }

        // Se não estiver no cache, busca do repositório
        user = await _repository.GetByEmailAsync(email);

        // Se encontrado, armazena no cache para consultas futuras
        if (user != null)
        {
            await _cache.SetUserAsync(user);
            _logger.LogDebug("Usuário com email {Email} armazenado no cache após busca no repositório", email);
        }

        return user;
    }

    public async Task<User> CreateAsync(User user)
    {
        // Cria o usuário no repositório
        var createdUser = await _repository.CreateAsync(user);
        
        // Armazena no cache
        await _cache.SetUserAsync(createdUser);
        _logger.LogDebug("Usuário {UserId} criado e armazenado no cache", createdUser.Id);
        
        return createdUser;
    }

    public async Task<bool> UpdateAsync(User user)
    {
        // Atualiza o usuário no repositório
        var updated = await _repository.UpdateAsync(user);
        
        // Se foi atualizado com sucesso, atualiza também no cache
        if (updated)
        {
            await _cache.SetUserAsync(user);
            _logger.LogDebug("Usuário {UserId} atualizado no cache", user.Id);
        }
        
        return updated;
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        // Para verificações de existência, vamos direto ao repositório
        // para garantir a resposta mais precisa
        return _repository.UsernameExistsAsync(username);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        // Para verificações de existência, vamos direto ao repositório
        // para garantir a resposta mais precisa
        return _repository.EmailExistsAsync(email);
    }
}