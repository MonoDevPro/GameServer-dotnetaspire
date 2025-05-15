using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Application.Services;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.In.Api;
using GameServer.AuthService.Infrastructure.Adapters.Out.Security;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Integration.Controllers;

public class AuthControllerIntegrationTests
{
    private readonly IAuthenticationService _authService;
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ITokenGenerator> _mockTokenGenerator;
    private readonly ILogger<AuthenticationService> _authServiceLogger;
    private readonly ILogger<AuthController> _controllerLogger;
    private readonly AuthController _controller;
    private readonly User _testUser;

    public AuthControllerIntegrationTests()
    {
        _testUser = TestHelper.CreateTestUser();
        _mockRepository = TestHelper.CreateMockUserRepository(_testUser);
        _mockTokenGenerator = TestHelper.CreateMockTokenGenerator();
        _authServiceLogger = TestHelper.CreateMockLogger<AuthenticationService>();
        _controllerLogger = TestHelper.CreateMockLogger<AuthController>();

        // Usar a implementação real do serviço para testes de integração
        _authService = new AuthenticationService(
            _mockRepository.Object,
            _mockTokenGenerator.Object,
            TestHelper.CreatePasswordHasher(),
            _authServiceLogger);

        _controller = new AuthController(_authService, _controllerLogger);
    }

    [Fact]
    public async Task RegisterAndLogin_FullFlow_ShouldSucceed()
    {
        // PARTE 1: REGISTRO
        // Arrange
        var registerRequest = new RegisterRequest("newuser", "new@example.com", "Password123!");

        var passwordHasher = TestHelper.CreatePasswordHasher();
        
        // Configurar o repositório para retornar o novo usuário ao fazer login
        var newUser = new User("newuser", "new@example.com", passwordHasher.HashPassword("Password123!"));
        _mockRepository.Setup(r => r.GetByUsernameAsync("newuser")).ReturnsAsync(newUser);

        // Act
        var registerResult = await _controller.Register(registerRequest);

        // Assert
        var okRegisterResult = Assert.IsType<OkObjectResult>(registerResult);
        var authResponse = Assert.IsType<AuthResponse>(okRegisterResult.Value);
        Assert.True(authResponse.Success);
        Assert.Equal("mock_jwt_token_for_tests", authResponse.Token);
        Assert.Contains("sucesso", authResponse.Message);

        // PARTE 2: LOGIN
        // Arrange
        var loginRequest = new LoginRequest("newuser", "Password123!");

        // Act
        var loginResult = await _controller.Login(loginRequest);

        // Assert
        var okLoginResult = Assert.IsType<OkObjectResult>(loginResult);
        var loginResponse = Assert.IsType<AuthResponse>(okLoginResult.Value);
        Assert.True(loginResponse.Success);
        Assert.Equal("mock_jwt_token_for_tests", loginResponse.Token);
        
        // PARTE 3: VALIDAÇÃO DE TOKEN
        // Act
        var validationResult = await _controller.ValidateToken("valid_token");

        // Assert
        var okValidationResult = Assert.IsType<OkObjectResult>(validationResult);
        Assert.True((bool)okValidationResult.Value);
    }

    [Fact]
    public async Task RegisterWithExistingUsername_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(_testUser.Username, "another@example.com", "Password123!");

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Contains("já está em uso", errorResponse.Message);
    }

    [Fact]
    public async Task LoginWithDisabledAccount_ShouldFail()
    {
        // Arrange
        // Cria um usuário desativado
        var disabledUser = TestHelper.CreateTestUser("disableduser", "disabled@example.com");
        disabledUser.Deactivate();
        
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByUsernameAsync("disableduser"))
            .ReturnsAsync(disabledUser);
        
        var authService = new AuthenticationService(
            mockRepo.Object,
            _mockTokenGenerator.Object,
            TestHelper.CreatePasswordHasher(),
            _authServiceLogger);
            
        var controller = new AuthController(authService, _controllerLogger);
        
        var loginRequest = new LoginRequest("disableduser", "Password123!");

        // Act
        var result = await controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        Assert.Contains("desativada", errorResponse.Message);
    }

    [Fact]
    public async Task TokenValidationFlow_ShouldBehaveProperly()
    {
        // Act - Validar token válido
        var validResult = await _controller.ValidateToken("valid_token");

        // Assert
        var okValidResult = Assert.IsType<OkObjectResult>(validResult);
        Assert.True((bool)okValidResult.Value);

        // Act - Validar token inválido
        var invalidResult = await _controller.ValidateToken("invalid_token");

        // Assert
        var okInvalidResult = Assert.IsType<OkObjectResult>(invalidResult);
        Assert.False((bool)okInvalidResult.Value);
    }
}