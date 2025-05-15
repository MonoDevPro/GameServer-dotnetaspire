using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.InMemory;

/// <summary>
/// Implementação em memória do repositório de usuários (para fins de demonstração)
/// Em um ambiente de produção, isso seria substituído por uma implementação de banco de dados real
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();
    private readonly Dictionary<string, Guid> _usernameIndex = new();
    private readonly Dictionary<string, Guid> _emailIndex = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        if (_usernameIndex.TryGetValue(username.ToLower(), out var userId))
        {
            return Task.FromResult(_users.GetValueOrDefault(userId));
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        if (_emailIndex.TryGetValue(email.ToLower(), out var userId))
        {
            return Task.FromResult(_users.GetValueOrDefault(userId));
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User> CreateAsync(User user)
    {
        _users[user.Id] = user;
        _usernameIndex[user.Username.ToLower()] = user.Id;
        _emailIndex[user.Email.ToLower()] = user.Id;
        return Task.FromResult(user);
    }

    public Task<bool> UpdateAsync(User user)
    {
        if (!_users.ContainsKey(user.Id))
        {
            return Task.FromResult(false);
        }

        // Remove old indexes
        var oldUser = _users[user.Id];
        _usernameIndex.Remove(oldUser.Username.ToLower());
        _emailIndex.Remove(oldUser.Email.ToLower());

        // Add new user and indexes
        _users[user.Id] = user;
        _usernameIndex[user.Username.ToLower()] = user.Id;
        _emailIndex[user.Email.ToLower()] = user.Id;

        return Task.FromResult(true);
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        return Task.FromResult(_usernameIndex.ContainsKey(username.ToLower()));
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return Task.FromResult(_emailIndex.ContainsKey(email.ToLower()));
    }
}