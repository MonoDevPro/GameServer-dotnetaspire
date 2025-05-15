using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Application.Services;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Domain.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ITokenGenerator> _mockTokenGenerator;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly User _testUser;

    public AuthenticationServiceTests()
    {
        _testUser = TestHelper.CreateTestUser();
        _mockRepository = TestHelper.CreateMockUserRepository(_testUser);
        _mockTokenGenerator = TestHelper.CreateMockTokenGenerator();
        _logger = TestHelper.CreateMockLogger<AuthenticationService>();

        _authService = new AuthenticationService(
            _mockRepository.Object,
            _mockTokenGenerator.Object,
            TestHelper.CreatePasswordHasher(),
            _logger);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldRegisterUser()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@example.com", "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("mock_jwt_token_for_tests", result.Token);
        Assert.Equal("Usuário registrado com sucesso!", result.Message);
        
        _mockRepository.Verify(r => r.UsernameExistsAsync("newuser"), Times.Once());
        _mockRepository.Verify(r => r.EmailExistsAsync("new@example.com"), Times.Once());
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once());
        _mockTokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Once());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequest(_testUser.Username, "new@example.com", "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("Nome de usuário já está em uso", result.Message);
        
        _mockRepository.Verify(r => r.UsernameExistsAsync(_testUser.Username), Times.Once());
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequest("newuser", _testUser.Email, "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("E-mail já está em uso", result.Message);
        
        _mockRepository.Verify(r => r.UsernameExistsAsync("newuser"), Times.Once());
        _mockRepository.Verify(r => r.EmailExistsAsync(_testUser.Email), Times.Once());
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidData_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequest("", "", "");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("Todos os campos são obrigatórios", result.Message);
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldLoginSuccessfully()
    {
        // Arrange
        var request = new LoginRequest(_testUser.Username, "Password123!");
        
        // Setup password verification to succeed
        // Esta configuração não é ideal, pois normalmente devemos testar o PasswordHasher também
        // Mas para fins de teste do comportamento do serviço, é aceitável

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("mock_jwt_token_for_tests", result.Token);
        Assert.Equal("Login realizado com sucesso!", result.Message);
        
        _mockRepository.Verify(r => r.GetByUsernameAsync(_testUser.Username), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once());
        _mockTokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Once());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ShouldReturnError()
    {
        // Arrange
        var request = new LoginRequest("nonexistentuser", "Password123!");
        _mockRepository.Setup(r => r.GetByUsernameAsync("nonexistentuser"))
            .ReturnsAsync((User)null);
        _mockRepository.Setup(r => r.GetByEmailAsync("nonexistentuser"))
            .ReturnsAsync((User)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("Credenciais inválidas", result.Message);
        
        _mockRepository.Verify(r => r.GetByUsernameAsync("nonexistentuser"), Times.Once());
        _mockRepository.Verify(r => r.GetByEmailAsync("nonexistentuser"), Times.Once());
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task LoginAsync_WithDisabledAccount_ShouldReturnError()
    {
        // Arrange
        var disabledUser = TestHelper.CreateTestUser();
        disabledUser.Deactivate();
        
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByUsernameAsync(disabledUser.Username))
            .ReturnsAsync(disabledUser);
            
        var authService = new AuthenticationService(
            mockRepo.Object,
            _mockTokenGenerator.Object,
            TestHelper.CreatePasswordHasher(),
            _logger);
            
        var request = new LoginRequest(disabledUser.Username, "Password123!");

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("Conta desativada", result.Message);
        
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task LoginAsync_WithBannedAccount_ShouldReturnError()
    {
        // Arrange
        var bannedUser = TestHelper.CreateTestUser();
        bannedUser.Ban(DateTime.UtcNow.AddDays(30), "Violação dos termos de uso");
        
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByUsernameAsync(bannedUser.Username))
            .ReturnsAsync(bannedUser);
            
        var authService = new AuthenticationService(
            mockRepo.Object,
            _mockTokenGenerator.Object,
            TestHelper.CreatePasswordHasher(),
            _logger);
            
        var request = new LoginRequest(bannedUser.Username, "Password123!");

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Token);
        Assert.Contains("Conta banida", result.Message);
        
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        string token = "valid_token";

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
        _mockTokenGenerator.Verify(t => t.ValidateToken(token), Times.Once());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        string token = "invalid_token";

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
        _mockTokenGenerator.Verify(t => t.ValidateToken(token), Times.Once());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        string token = "expired_token";

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
        _mockTokenGenerator.Verify(t => t.ValidateToken(token), Times.Once());
    }

    [Fact]
    public async Task GetTokenExpirationReason_WithExpiredToken_ShouldReturnExpirationMessage()
    {
        // Arrange
        string token = "expired_token";

        // Act
        var result = await _authService.GetTokenExpirationReasonAsync(token);

        // Assert
        Assert.Equal("Token expirado", result);
        _mockTokenGenerator.Verify(t => t.GetExpirationReason(token), Times.Once());
    }

    [Fact]
    public async Task GetTokenExpirationReasonAsync_WithExpiredToken_ShouldReturnExpirationReason()
    {
        // Arrange
        string token = "expired_token";

        // Act
        var reason = await _authService.GetTokenExpirationReasonAsync(token);

        // Assert
        Assert.Equal("Token expirado", reason);
        _mockTokenGenerator.Verify(t => t.GetExpirationReason(token), Times.Once());
    }

    [Fact]
    public async Task GetTokenExpirationReasonAsync_WithInvalidToken_ShouldReturnInvalidReason()
    {
        // Arrange
        string token = "invalid_token";

        // Act
        var reason = await _authService.GetTokenExpirationReasonAsync(token);

        // Assert
        Assert.Equal("Token inválido", reason);
        _mockTokenGenerator.Verify(t => t.GetExpirationReason(token), Times.Once());
    }
}