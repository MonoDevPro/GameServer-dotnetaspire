using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.Out.Persistence;

public class CachedUserRepositoryTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IUserCache> _mockCache;
    private readonly ILogger<CachedUserRepository> _mockLogger;
    private readonly CachedUserRepository _cachedRepository;
    private readonly User _testUser;

    public CachedUserRepositoryTests()
    {
        _testUser = TestHelper.CreateTestUser();
        _mockRepository = TestHelper.CreateMockUserRepository(_testUser);
        _mockCache = TestHelper.CreateMockUserCache(_testUser);
        _mockLogger = TestHelper.CreateMockLogger<CachedUserRepository>();

        _cachedRepository = new CachedUserRepository(
            _mockRepository.Object,
            _mockCache.Object,
            _mockLogger);
    }

    [Fact]
    public async Task GetByIdAsync_WithCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _cachedRepository.GetByIdAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Id, result.Id);
        
        // Verificar que o cache foi consultado e o repositório não foi chamado
        _mockCache.Verify(c => c.GetUserByIdAsync(_testUser.Id), Times.Once());
        _mockRepository.Verify(r => r.GetByIdAsync(_testUser.Id), Times.Never());
    }

    [Fact]
    public async Task GetByIdAsync_WithCacheMiss_ShouldGetFromRepositoryAndCacheResult()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByIdAsync(_testUser.Id))
            .ReturnsAsync((User)null);

        // Act
        var result = await _cachedRepository.GetByIdAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Id, result.Id);
        
        // Verificar que o cache foi consultado, o repositório foi chamado
        // e o resultado foi armazenado no cache
        _mockCache.Verify(c => c.GetUserByIdAsync(_testUser.Id), Times.Once());
        _mockRepository.Verify(r => r.GetByIdAsync(_testUser.Id), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(It.Is<User>(u => u.Id == _testUser.Id)), Times.Once());
    }

    [Fact]
    public async Task GetByUsernameAsync_WithCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByUsernameAsync(_testUser.Username))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _cachedRepository.GetByUsernameAsync(_testUser.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Username, result.Username);
        
        // Verificar que o cache foi consultado e o repositório não foi chamado
        _mockCache.Verify(c => c.GetUserByUsernameAsync(_testUser.Username), Times.Once());
        _mockRepository.Verify(r => r.GetByUsernameAsync(_testUser.Username), Times.Never());
    }

    [Fact]
    public async Task GetByUsernameAsync_WithCacheMiss_ShouldGetFromRepositoryAndCacheResult()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByUsernameAsync(_testUser.Username))
            .ReturnsAsync((User)null);

        // Act
        var result = await _cachedRepository.GetByUsernameAsync(_testUser.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Username, result.Username);
        
        // Verificar que o cache foi consultado, o repositório foi chamado
        // e o resultado foi armazenado no cache
        _mockCache.Verify(c => c.GetUserByUsernameAsync(_testUser.Username), Times.Once());
        _mockRepository.Verify(r => r.GetByUsernameAsync(_testUser.Username), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(It.Is<User>(u => u.Username == _testUser.Username)), Times.Once());
    }

    [Fact]
    public async Task GetByEmailAsync_WithCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _cachedRepository.GetByEmailAsync(_testUser.Email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Email, result.Email);
        
        // Verificar que o cache foi consultado e o repositório não foi chamado
        _mockCache.Verify(c => c.GetUserByEmailAsync(_testUser.Email), Times.Once());
        _mockRepository.Verify(r => r.GetByEmailAsync(_testUser.Email), Times.Never());
    }

    [Fact]
    public async Task GetByEmailAsync_WithCacheMiss_ShouldGetFromRepositoryAndCacheResult()
    {
        // Arrange
        _mockCache.Setup(c => c.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync((User)null);

        // Act
        var result = await _cachedRepository.GetByEmailAsync(_testUser.Email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Email, result.Email);
        
        // Verificar que o cache foi consultado, o repositório foi chamado
        // e o resultado foi armazenado no cache
        _mockCache.Verify(c => c.GetUserByEmailAsync(_testUser.Email), Times.Once());
        _mockRepository.Verify(r => r.GetByEmailAsync(_testUser.Email), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(It.Is<User>(u => u.Email == _testUser.Email)), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateInRepositoryAndCache()
    {
        // Arrange
        var newUser = new User("newuser", "new@example.com", "hash");

        // Act
        var result = await _cachedRepository.CreateAsync(newUser);

        // Assert
        Assert.NotNull(result);
        
        // Verificar que o repositório foi chamado para criar e o resultado foi armazenado no cache
        _mockRepository.Verify(r => r.CreateAsync(newUser), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(It.IsAny<User>()), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRepositoryAndCache()
    {
        // Arrange
        _mockRepository.Setup(r => r.UpdateAsync(_testUser))
            .ReturnsAsync(true);

        // Act
        var result = await _cachedRepository.UpdateAsync(_testUser);

        // Assert
        Assert.True(result);
        
        // Verificar que o repositório foi atualizado e o cache também
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryUpdateFails_ShouldNotUpdateCache()
    {
        // Arrange
        _mockRepository.Setup(r => r.UpdateAsync(_testUser))
            .ReturnsAsync(false);

        // Act
        var result = await _cachedRepository.UpdateAsync(_testUser);

        // Assert
        Assert.False(result);
        
        // Verificar que o repositório tentou atualizar, mas o cache não foi chamado
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
        _mockCache.Verify(c => c.SetUserAsync(_testUser), Times.Never());
    }

    [Fact]
    public async Task UsernameExistsAsync_ShouldQueryRepository()
    {
        // Act
        var result = await _cachedRepository.UsernameExistsAsync(_testUser.Username);

        // Assert
        Assert.True(result);
        
        // Para verificações de existência, sempre consultamos o repositório
        // para garantir a resposta mais precisa
        _mockRepository.Verify(r => r.UsernameExistsAsync(_testUser.Username), Times.Once());
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldQueryRepository()
    {
        // Act
        var result = await _cachedRepository.EmailExistsAsync(_testUser.Email);

        // Assert
        Assert.True(result);
        
        // Para verificações de existência, sempre consultamos o repositório
        // para garantir a resposta mais precisa
        _mockRepository.Verify(r => r.EmailExistsAsync(_testUser.Email), Times.Once());
    }
}