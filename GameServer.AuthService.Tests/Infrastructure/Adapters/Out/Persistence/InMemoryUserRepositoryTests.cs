using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.InMemory;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.Out.Persistence;

public class InMemoryUserRepositoryTests
{
    private readonly InMemoryUserRepository _repository;
    private readonly ILogger<InMemoryUserRepository> _logger;
    private readonly User _testUser;

    public InMemoryUserRepositoryTests()
    {
        _logger = new Mock<ILogger<InMemoryUserRepository>>().Object;
        _repository = new InMemoryUserRepository();
        
        // Criar um usuário para os testes
        _testUser = new User("testuser", "test@example.com", "PasswordHash");
    }

    [Fact]
    public async Task CreateAsync_ShouldAddUserToRepository()
    {
        // Act
        var createdUser = await _repository.CreateAsync(_testUser);

        // Assert
        Assert.NotNull(createdUser);
        Assert.Equal(_testUser.Username, createdUser.Username);
        Assert.Equal(_testUser.Email, createdUser.Email);
        
        // Verificar se o usuário foi realmente armazenado
        var retrievedUser = await _repository.GetByIdAsync(createdUser.Id);
        Assert.NotNull(retrievedUser);
        Assert.Equal(_testUser.Username, retrievedUser.Username);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var createdUser = await _repository.CreateAsync(_testUser);

        // Act
        var user = await _repository.GetByIdAsync(createdUser.Id);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(createdUser.Id, user.Id);
        Assert.Equal(_testUser.Username, user.Username);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var user = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Arrange
        await _repository.CreateAsync(_testUser);

        // Act
        var user = await _repository.GetByUsernameAsync(_testUser.Username);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(_testUser.Username, user.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithInvalidUsername_ShouldReturnNull()
    {
        // Act
        var user = await _repository.GetByUsernameAsync("nonexistentuser");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        await _repository.CreateAsync(_testUser);

        // Act
        var user = await _repository.GetByEmailAsync(_testUser.Email);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(_testUser.Email, user.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Act
        var user = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var createdUser = await _repository.CreateAsync(_testUser);
        
        // Alterando dados do usuário
        createdUser.UpdateEmail("updated@example.com");

        // Act
        var result = await _repository.UpdateAsync(createdUser);

        // Assert
        Assert.True(result);
        
        // Verificar se os dados foram atualizados
        var updatedUser = await _repository.GetByIdAsync(createdUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("updated@example.com", updatedUser.Email);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentUser = new User("nonexistent", "nonexistent@example.com", "hash");
        
        // Utilizando reflexão para definir o ID diretamente para um GUID específico
        var idProperty = typeof(User).GetProperty("Id");
        idProperty.SetValue(nonExistentUser, Guid.NewGuid());

        // Act
        var result = await _repository.UpdateAsync(nonExistentUser);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithExistingUsername_ShouldReturnTrue()
    {
        // Arrange
        await _repository.CreateAsync(_testUser);

        // Act
        var exists = await _repository.UsernameExistsAsync(_testUser.Username);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithNonExistingUsername_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.UsernameExistsAsync("nonexistentuser");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        await _repository.CreateAsync(_testUser);

        // Act
        var exists = await _repository.EmailExistsAsync(_testUser.Email);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.False(exists);
    }
}