using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Infrastructure.Adapters.In.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.In.Api;

public class AccountControllerTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<ITokenGenerator> _mockTokenGenerator;
    private readonly Mock<IUserCache> _mockCache;
    private readonly ILogger<AccountController> _logger;
    private readonly AccountController _controller;
    private readonly Guid _userId = Guid.Parse("12345678-1234-1234-1234-123456789012");

    public AccountControllerTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockTokenGenerator = new Mock<ITokenGenerator>();
        _mockCache = new Mock<IUserCache>();
        _logger = new Mock<ILogger<AccountController>>().Object;
        
        _controller = new AccountController(
            _mockAccountService.Object,
            _mockTokenGenerator.Object,
            _mockCache.Object,
            _logger);
            
        // Setup para controller receber token nos testes
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid_token";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Setup para validação de token
        _mockTokenGenerator.Setup(t => t.GetUserIdFromToken("valid_token"))
            .Returns(_userId);
    }

    [Fact]
    public async Task GetMyAccount_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var accountResponse = new AccountResponse(
            _userId, 
            "testuser", 
            "test@example.com",
            true, 
            DateTime.UtcNow.AddDays(-30), 
            null,
            false,
            null);
            
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AccountResponse>(okResult.Value);
        
        Assert.Equal(_userId, returnValue.Id);
        Assert.Equal("testuser", returnValue.Username);
        Assert.Equal("test@example.com", returnValue.Email);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task GetMyAccount_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockTokenGenerator.Setup(t => t.GetUserIdFromToken("valid_token"))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        
        Assert.Equal("Token inválido ou expirado.", returnValue.Message);
    }

    [Fact]
    public async Task GetMyAccount_WithNonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync((AccountResponse)null);

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        
        Assert.Equal("Conta não encontrada.", returnValue.Message);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task UpdateMyAccount_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateAccountRequest("newemail@example.com", "OldPassword123", "NewPassword456");
        var accountResponse = new AccountResponse(
            _userId, 
            "testuser", 
            "test@example.com",
            true, 
            DateTime.UtcNow.AddDays(-30), 
            null,
            false,
            null);
            
        _mockAccountService.Setup(s => s.UpdateAccountAsync(_userId, request))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _controller.UpdateMyAccount(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AccountResponse>(okResult.Value);
        
        Assert.Equal("test@example.com", returnValue.Email);
        
        _mockAccountService.Verify(s => s.UpdateAccountAsync(_userId, request), Times.Once());
    }

    [Fact]
    public async Task UpdateMyAccount_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockTokenGenerator.Setup(t => t.GetUserIdFromToken("valid_token"))
            .Returns((Guid?)null);
            
        var request = new UpdateAccountRequest("newemail@example.com", "OldPassword123", "NewPassword456");

        // Act
        var result = await _controller.UpdateMyAccount(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        
        Assert.Equal("Token inválido ou expirado.", returnValue.Message);
    }

    [Fact]
    public async Task UpdateMyAccount_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpdateAccountRequest("invalid_email", "Wrong", "Short");
        _mockAccountService.Setup(s => s.UpdateAccountAsync(_userId, request))
            .ThrowsAsync(new InvalidOperationException("Email inválido"));

        // Act
        var result = await _controller.UpdateMyAccount(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        
        Assert.Equal("Email inválido", returnValue.Message);
        
        _mockAccountService.Verify(s => s.UpdateAccountAsync(_userId, request), Times.Once());
    }

    [Fact]
    public async Task GetAccount_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var accountResponse = new AccountResponse(
            _userId, 
            "testuser", 
            "test@example.com",
            true, 
            DateTime.UtcNow.AddDays(-30), 
            null,
            false,
            null);
            
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _controller.GetAccount(_userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AccountResponse>(okResult.Value);
        
        Assert.Equal(_userId, returnValue.Id);
        Assert.Equal("testuser", returnValue.Username);
        Assert.Equal("test@example.com", returnValue.Email);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task GetAccount_WithNonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync((AccountResponse)null);

        // Act
        var result = await _controller.GetAccount(_userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        
        Assert.Equal("Conta não encontrada.", returnValue.Message);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task BanAccount_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new BanRequest(DateTime.UtcNow.AddDays(30), "Violação dos termos de uso");
        
        _mockAccountService.Setup(s => s.BanAccountAsync(_userId, request.Until, request.Reason))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BanAccount(_userId, request);

        // Assert
        Assert.IsType<OkResult>(result);
        
        _mockAccountService.Verify(s => s.BanAccountAsync(_userId, request.Until, request.Reason), Times.Once());
    }

    [Fact]
    public async Task BanAccount_WithEmptyReason_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new BanRequest(DateTime.UtcNow.AddDays(30), "");

        // Act
        var result = await _controller.BanAccount(_userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        
        Assert.Equal("O motivo do banimento é obrigatório.", returnValue.Message);
    }

    [Fact]
    public async Task BanAccount_WhenBanFails_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new BanRequest(DateTime.UtcNow.AddDays(30), "Violação dos termos de uso");
        
        _mockAccountService.Setup(s => s.BanAccountAsync(_userId, request.Until, request.Reason))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.BanAccount(_userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        
        Assert.Equal("Não foi possível banir o usuário.", returnValue.Message);
        
        _mockAccountService.Verify(s => s.BanAccountAsync(_userId, request.Until, request.Reason), Times.Once());
    }

    [Fact]
    public async Task UnbanAccount_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        _mockAccountService.Setup(s => s.UnbanAccountAsync(_userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UnbanAccount(_userId);

        // Assert
        Assert.IsType<OkResult>(result);
        
        _mockAccountService.Verify(s => s.UnbanAccountAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task ActivateAccount_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        _mockAccountService.Setup(s => s.ActivateAccountAsync(_userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ActivateAccount(_userId);

        // Assert
        Assert.IsType<OkResult>(result);
        
        _mockAccountService.Verify(s => s.ActivateAccountAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task DeactivateAccount_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        _mockAccountService.Setup(s => s.DeactivateAccountAsync(_userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeactivateAccount(_userId);

        // Assert
        Assert.IsType<OkResult>(result);
        
        _mockAccountService.Verify(s => s.DeactivateAccountAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task ClearUserCache_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var accountResponse = new AccountResponse(
            _userId, 
            "testuser", 
            "test@example.com",
            true, 
            DateTime.UtcNow.AddDays(-30), 
            null,
            false,
            null);
            
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _controller.ClearUserCache(_userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
        _mockCache.Verify(c => c.RemoveUserAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task ClearUserCache_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_userId))
            .ReturnsAsync((AccountResponse)null);

        // Act
        var result = await _controller.ClearUserCache(_userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        
        Assert.Equal("Usuário não encontrado.", returnValue.Message);
        
        _mockAccountService.Verify(s => s.GetAccountDetailsAsync(_userId), Times.Once());
        _mockCache.Verify(c => c.RemoveUserAsync(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task ClearMyCache_WithValidToken_ShouldReturnOk()
    {
        // Act
        var result = await _controller.ClearMyCache();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        _mockCache.Verify(c => c.RemoveUserAsync(_userId), Times.Once());
    }

    [Fact]
    public async Task ClearMyCache_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockTokenGenerator.Setup(t => t.GetUserIdFromToken("valid_token"))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.ClearMyCache();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        
        Assert.Equal("Token inválido ou expirado.", returnValue.Message);
        
        _mockCache.Verify(c => c.RemoveUserAsync(It.IsAny<Guid>()), Times.Never());
    }
}