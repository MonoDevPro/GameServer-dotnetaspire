using GameServer.AuthService.Domain.Entities;
using GameServer.AuthService.Infrastructure.Adapters.Out.Authentication;
using GameServer.AuthService.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameServer.AuthService.Tests.Infrastructure.Adapters.Out.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenGenerator> _logger;
    private readonly User _testUser;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            Key = "TestingKey_MustBe_AtLeast_32_Characters_Long_For_Security",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresInMinutes = 60
        };

        var options = Options.Create(_jwtSettings);
        _logger = new Mock<ILogger<JwtTokenGenerator>>().Object;
        _tokenGenerator = new JwtTokenGenerator(options, _logger);
        
        _testUser = TestHelper.CreateTestUser();
    }

    [Fact]
    public void GenerateToken_ShouldCreateValidJwtToken()
    {
        // Act
        var token = _tokenGenerator.GenerateToken(_testUser);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Verify token is valid JWT format
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
        
        // Parse and analyze token
        var jwtToken = handler.ReadJwtToken(token);
        
        // Verify issuer
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        
        // Verify audience
        Assert.Equal(_jwtSettings.Audience, jwtToken.Audiences.FirstOrDefault());
        
        // Extract all claims
        var claims = jwtToken.Claims.ToList();
        
        // Check for user ID claim (try both formats)
        var nameIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) 
                        ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        
        // Check for username claim (try both formats)
        var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name) 
                      ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
        
        // Check for email claim (try both formats)
        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email) 
                      ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        
        // Verify claims exist
        Assert.NotNull(nameIdClaim);
        Assert.NotNull(nameClaim);
        Assert.NotNull(emailClaim);
        
        // Verify claim values match test user
        Assert.Equal(_testUser.Id.ToString(), nameIdClaim.Value);
        Assert.Equal(_testUser.Username, nameClaim.Value);
        Assert.Equal(_testUser.Email, emailClaim.Value);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = _tokenGenerator.GenerateToken(_testUser);

        // Act
        var result = _tokenGenerator.ValidateToken(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Act
        var result = _tokenGenerator.ValidateToken("invalid.token.format");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnFalse()
    {
        // Act
        var result = _tokenGenerator.ValidateToken("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var token = _tokenGenerator.GenerateToken(_testUser);

        // Act
        var userId = _tokenGenerator.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(userId);
        Assert.Equal(_testUser.Id, userId);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var userId = _tokenGenerator.GetUserIdFromToken("invalid.token.format");

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void GetUserIdFromToken_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var userId = _tokenGenerator.GetUserIdFromToken("");

        // Assert
        Assert.Null(userId);
    }
}