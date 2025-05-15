using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Infrastructure.Adapters.In.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.In.Api;

public class AuthControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _logger = new Mock<ILogger<AuthController>>().Object;
        _controller = new AuthController(_mockAuthService.Object, _logger);
    }

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new RegisterRequest("testuser", "test@example.com", "Password123!");
        var authResponse = new AuthResponse(true, "Usuário registrado com sucesso!", "token123");
        
        _mockAuthService.Setup(s => s.RegisterAsync(request))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.True(returnValue.Success);
        Assert.Equal(authResponse.Token, returnValue.Token);
        Assert.Equal(authResponse.Message, returnValue.Message);
        
        _mockAuthService.Verify(s => s.RegisterAsync(request), Times.Once());
    }

    [Fact]
    public async Task Register_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("", "", "");
        var authResponse = new AuthResponse(false, "Todos os campos são obrigatórios", "");
        
        _mockAuthService.Setup(s => s.RegisterAsync(request))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        
        Assert.Equal(authResponse.Message, returnValue.Message);
        
        _mockAuthService.Verify(s => s.RegisterAsync(request), Times.Once());
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange
        var request = new LoginRequest("testuser", "Password123!");
        var authResponse = new AuthResponse(true, "Login realizado com sucesso!", "token123");
        
        _mockAuthService.Setup(s => s.LoginAsync(request))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.True(returnValue.Success);
        Assert.Equal(authResponse.Token, returnValue.Token);
        Assert.Equal(authResponse.Message, returnValue.Message);
        
        _mockAuthService.Verify(s => s.LoginAsync(request), Times.Once());
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("testuser", "WrongPassword");
        var authResponse = new AuthResponse(false, "Credenciais inválidas", "");
        
        _mockAuthService.Setup(s => s.LoginAsync(request))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        
        Assert.Equal(authResponse.Message, returnValue.Message);
        
        _mockAuthService.Verify(s => s.LoginAsync(request), Times.Once());
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        string token = "valid_token";
        _mockAuthService.Setup(s => s.ValidateTokenAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateToken(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<bool>(okResult.Value);
        
        Assert.True(returnValue);
        
        _mockAuthService.Verify(s => s.ValidateTokenAsync(token), Times.Once());
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_ShouldReturnOk()
    {
        // Arrange
        string token = "invalid_token";
        _mockAuthService.Setup(s => s.ValidateTokenAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateToken(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<bool>(okResult.Value);
        
        Assert.False(returnValue);
        
        _mockAuthService.Verify(s => s.ValidateTokenAsync(token), Times.Once());
    }
}