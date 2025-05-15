using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameServer.AuthService.Tests.Helpers;

/// <summary>
/// Classe auxiliar para testes
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Cria um usuário para testes
    /// </summary>
    public static User CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        var passwordHasher = new PasswordHasher();

        // Criamos um hash válido para a senha de teste "Password123!"
        var validHash = passwordHasher.HashPassword("Password123!");
        var user = new User(username, email, validHash);
        
        // Utilizamos reflexão para definir o ID diretamente
        var idProperty = typeof(User).GetProperty("Id");
        if (idProperty != null)
        {
            idProperty.SetValue(user, Guid.Parse("11111111-2222-3333-4444-555555555555"));
        }
        
        // Definimos a data de criação no passado
        var createdAtProperty = typeof(User).GetProperty("CreatedAt");
        if (createdAtProperty != null)
        {
            createdAtProperty.SetValue(user, DateTime.UtcNow.AddDays(-30));
        }
        
        return user;
    }

    public static IPasswordHasher CreatePasswordHasher()
    {
        return new PasswordHasher();
    }
    
    /// <summary>
    /// Cria um mock de IUserRepository para testes
    /// </summary>
    public static Mock<IUserRepository> CreateMockUserRepository(User testUser)
    {
        var mock = new Mock<IUserRepository>();
        
        // Configuração para GetById
        mock.Setup(r => r.GetByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);
            
        // Configuração para GetByUsername
        mock.Setup(r => r.GetByUsernameAsync(testUser.Username))
            .ReturnsAsync(testUser);
            
        // Configuração para GetByEmail
        mock.Setup(r => r.GetByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);
            
        // Configuração para UsernameExists
        mock.Setup(r => r.UsernameExistsAsync(testUser.Username))
            .ReturnsAsync(true);
        mock.Setup(r => r.UsernameExistsAsync(It.Is<string>(s => s != testUser.Username)))
            .ReturnsAsync(false);
            
        // Configuração para EmailExists
        mock.Setup(r => r.EmailExistsAsync(testUser.Email))
            .ReturnsAsync(true);
        mock.Setup(r => r.EmailExistsAsync(It.Is<string>(s => s != testUser.Email)))
            .ReturnsAsync(false);
            
        // Configuração para Create
        mock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
            
        // Configuração para Update
        mock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);
            
        return mock;
    }
    
    /// <summary>
    /// Cria um mock de ITokenGenerator para testes
    /// </summary>
    public static Mock<ITokenGenerator> CreateMockTokenGenerator()
    {
        var mock = new Mock<ITokenGenerator>();
        
        // Configuração para GenerateToken
        mock.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("mock_jwt_token_for_tests");
            
        // Configuração para ValidateToken
        mock.Setup(t => t.ValidateToken("valid_token"))
            .Returns(true);
        mock.Setup(t => t.ValidateToken(It.Is<string>(s => s != "valid_token")))
            .Returns(false);
        
        // Configuração específica para token expirado
        mock.Setup(t => t.ValidateToken("expired_token"))
            .Returns(false);
        mock.Setup(t => t.GetExpirationReason("expired_token"))
            .Returns("Token expirado");
        // Configuração para token inválido
        mock.Setup(t => t.GetExpirationReason(It.Is<string>(s => s != "valid_token" && s != "expired_token")))
            .Returns("Token inválido");
            
        // Configuração para GetUserIdFromToken
        mock.Setup(t => t.GetUserIdFromToken("valid_token"))
            .Returns(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        mock.Setup(t => t.GetUserIdFromToken(It.Is<string>(s => s != "valid_token")))
            .Returns((Guid?)null);
        // Configuração específica para token expirado
        mock.Setup(t => t.GetUserIdFromToken("expired_token"))
            .Returns((Guid?)null);
            
        return mock;
    }
    
    /// <summary>
    /// Cria um mock de IUserCache para testes
    /// </summary>
    public static Mock<IUserCache> CreateMockUserCache(User testUser)
    {
        var mock = new Mock<IUserCache>();
        
        // Configuração para GetUserById
        mock.Setup(c => c.GetUserByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);
            
        // Configuração para GetUserByUsername
        mock.Setup(c => c.GetUserByUsernameAsync(testUser.Username))
            .ReturnsAsync(testUser);
            
        // Configuração para GetUserByEmail
        mock.Setup(c => c.GetUserByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);
            
        // Configuração para SetUser
        mock.Setup(c => c.SetUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
            
        // Configuração para RemoveUser
        mock.Setup(c => c.RemoveUserAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
            
        // Configuração para AddValidToken
        mock.Setup(c => c.AddValidTokenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
            
        // Configuração para IsTokenValid
        mock.Setup(c => c.IsTokenValidAsync("valid_token"))
            .ReturnsAsync(true);
        mock.Setup(c => c.IsTokenValidAsync(It.Is<string>(s => s != "valid_token")))
            .ReturnsAsync(false);
            
        // Configuração para InvalidateToken
        mock.Setup(c => c.InvalidateTokenAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
            
        return mock;
    }
    
    /// <summary>
    /// Cria um mock de ILogger para testes
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }
}