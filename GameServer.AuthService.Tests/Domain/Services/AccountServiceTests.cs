using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Application.Services;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Domain.Services;

public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly ILogger<AccountService> _logger;
    private readonly AccountService _accountService;
    private readonly User _testUser;

    public AccountServiceTests()
    {
        _testUser = TestHelper.CreateTestUser();
        _mockRepository = TestHelper.CreateMockUserRepository(_testUser);
        _logger = TestHelper.CreateMockLogger<AccountService>();

        _accountService = new AccountService(
            _mockRepository.Object,
            TestHelper.CreatePasswordHasher(),
            _logger);
    }

    [Fact]
    public async Task GetAccountDetailsAsync_WithValidId_ShouldReturnAccountDetails()
    {
        // Arrange
        var userId = _testUser.Id;

        // Act
        var result = await _accountService.GetAccountDetailsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUser.Id, result.Id);
        Assert.Equal(_testUser.Username, result.Username);
        Assert.Equal(_testUser.Email, result.Email);
        Assert.Equal(_testUser.IsActive, result.IsActive);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
    }

    [Fact]
    public async Task GetAccountDetailsAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null);

        // Act
        var result = await _accountService.GetAccountDetailsAsync(userId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
    }

    [Fact]
    public async Task UpdateAccountAsync_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var userId = _testUser.Id;
        var request = new UpdateAccountRequest("newemail@example.com", null, null);

        // Act
        var result = await _accountService.UpdateAccountAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newemail@example.com", _testUser.Email);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.EmailExistsAsync("newemail@example.com"), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task UpdateAccountAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var userId = _testUser.Id;
        var request = new UpdateAccountRequest("existing@example.com", null, null);
        _mockRepository.Setup(r => r.EmailExistsAsync("existing@example.com")).ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _accountService.UpdateAccountAsync(userId, request));
        
        Assert.Contains("O e-mail já está em uso", exception.Message);
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Never());
    }

    [Fact]
    public async Task UpdateAccountAsync_WithValidPassword_ShouldUpdatePassword()
    {
        // Arrange
        var userId = _testUser.Id;
        var originalPasswordHash = _testUser.PasswordHash;
        var request = new UpdateAccountRequest(null, "Password123!", "NewPassword456!");

        // Act
        var result = await _accountService.UpdateAccountAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(originalPasswordHash, _testUser.PasswordHash);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task UpdateAccountAsync_WithIncorrectCurrentPassword_ShouldThrowException()
    {
        // Arrange
        var userId = _testUser.Id;
        var request = new UpdateAccountRequest(null, "WrongPassword", "NewPassword456!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _accountService.UpdateAccountAsync(userId, request));
        
        Assert.Contains("Senha atual incorreta", exception.Message);
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Never());
    }

    [Fact]
    public async Task BanAccountAsync_WithValidData_ShouldBanAccount()
    {
        // Arrange
        var userId = _testUser.Id;
        var banUntil = DateTime.UtcNow.AddDays(30);
        var reason = "Violação dos termos de serviço";

        // Act
        var result = await _accountService.BanAccountAsync(userId, banUntil, reason);

        // Assert
        Assert.True(result);
        Assert.True(_testUser.IsBanned);
        Assert.Equal(banUntil, _testUser.BannedUntil);
        Assert.Equal(reason, _testUser.BanReason);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task BanAccountAsync_WithInvalidUserId_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null);
        var banUntil = DateTime.UtcNow.AddDays(30);
        var reason = "Violação dos termos de serviço";

        // Act
        var result = await _accountService.BanAccountAsync(userId, banUntil, reason);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task UnbanAccountAsync_WithValidData_ShouldUnbanAccount()
    {
        // Arrange
        var userId = _testUser.Id;
        _testUser.Ban(DateTime.UtcNow.AddDays(30), "Teste");

        // Act
        var result = await _accountService.UnbanAccountAsync(userId);

        // Assert
        Assert.True(result);
        Assert.False(_testUser.IsBanned);
        Assert.Null(_testUser.BannedUntil);
        Assert.Null(_testUser.BanReason);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task DeactivateAccountAsync_WithValidData_ShouldDeactivateAccount()
    {
        // Arrange
        var userId = _testUser.Id;

        // Act
        var result = await _accountService.DeactivateAccountAsync(userId);

        // Assert
        Assert.True(result);
        Assert.False(_testUser.IsActive);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }

    [Fact]
    public async Task ActivateAccountAsync_WithValidData_ShouldActivateAccount()
    {
        // Arrange
        var userId = _testUser.Id;
        _testUser.Deactivate();

        // Act
        var result = await _accountService.ActivateAccountAsync(userId);

        // Assert
        Assert.True(result);
        Assert.True(_testUser.IsActive);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(_testUser), Times.Once());
    }
}