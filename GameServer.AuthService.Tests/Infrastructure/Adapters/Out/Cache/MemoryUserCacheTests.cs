using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Cache;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.Out.Cache;

public class MemoryUserCacheTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryUserCache> _logger;
    private readonly UserCacheOptions _options;
    private readonly MemoryUserCache _userCache;
    private readonly User _testUser;

    public MemoryUserCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<MemoryUserCache>>().Object;
        _options = new UserCacheOptions
        {
            UserCacheExpirationMinutes = 30,
            SizeLimit = 1000
        };
        
        var optionsWrapper = Options.Create(_options);
        _userCache = new MemoryUserCache(_memoryCache, _logger, optionsWrapper);
        
        _testUser = TestHelper.CreateTestUser();
    }

    [Fact]
    public async Task SetUserAsync_ShouldStoreUserInCache()
    {
        // Act
        await _userCache.SetUserAsync(_testUser);

        // Assert
        var cachedUser = await _userCache.GetUserByIdAsync(_testUser.Id);
        Assert.NotNull(cachedUser);
        Assert.Equal(_testUser.Id, cachedUser.Id);
        Assert.Equal(_testUser.Username, cachedUser.Username);
        Assert.Equal(_testUser.Email, cachedUser.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Act
        var cachedUser = await _userCache.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(cachedUser);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_AfterSettingUser_ShouldReturnUser()
    {
        // Arrange
        await _userCache.SetUserAsync(_testUser);

        // Act
        var cachedUser = await _userCache.GetUserByUsernameAsync(_testUser.Username);

        // Assert
        Assert.NotNull(cachedUser);
        Assert.Equal(_testUser.Id, cachedUser.Id);
        Assert.Equal(_testUser.Username, cachedUser.Username);
    }

    [Fact]
    public async Task GetUserByEmailAsync_AfterSettingUser_ShouldReturnUser()
    {
        // Arrange
        await _userCache.SetUserAsync(_testUser);

        // Act
        var cachedUser = await _userCache.GetUserByEmailAsync(_testUser.Email);

        // Assert
        Assert.NotNull(cachedUser);
        Assert.Equal(_testUser.Id, cachedUser.Id);
        Assert.Equal(_testUser.Email, cachedUser.Email);
    }

    [Fact]
    public async Task RemoveUserAsync_ShouldRemoveUserFromCache()
    {
        // Arrange
        await _userCache.SetUserAsync(_testUser);
        
        // Verify user is in cache
        var userBeforeRemoval = await _userCache.GetUserByIdAsync(_testUser.Id);
        Assert.NotNull(userBeforeRemoval);

        // Act
        await _userCache.RemoveUserAsync(_testUser.Id);

        // Assert
        var userAfterRemoval = await _userCache.GetUserByIdAsync(_testUser.Id);
        Assert.Null(userAfterRemoval);
        
        // Check username and email indices are removed too
        var userByUsername = await _userCache.GetUserByUsernameAsync(_testUser.Username);
        Assert.Null(userByUsername);
        
        var userByEmail = await _userCache.GetUserByEmailAsync(_testUser.Email);
        Assert.Null(userByEmail);
    }

    [Fact]
    public async Task AddValidTokenAsync_ShouldStoreTokenInCache()
    {
        // Arrange
        var token = "test_token";
        var expiration = DateTime.UtcNow.AddHours(1);

        // Act
        await _userCache.AddValidTokenAsync(token, _testUser.Id, expiration);

        // Assert
        var isValid = await _userCache.IsTokenValidAsync(token);
        Assert.True(isValid);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Act
        var isValid = await _userCache.IsTokenValidAsync("nonexistent_token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldRemoveTokenFromCache()
    {
        // Arrange
        var token = "test_token";
        var expiration = DateTime.UtcNow.AddHours(1);
        await _userCache.AddValidTokenAsync(token, _testUser.Id, expiration);
        
        // Verify token is valid
        var isValidBeforeInvalidation = await _userCache.IsTokenValidAsync(token);
        Assert.True(isValidBeforeInvalidation);

        // Act
        await _userCache.InvalidateTokenAsync(token);

        // Assert
        var isValidAfterInvalidation = await _userCache.IsTokenValidAsync(token);
        Assert.False(isValidAfterInvalidation);
    }
}