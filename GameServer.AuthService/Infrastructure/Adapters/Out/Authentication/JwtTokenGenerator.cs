using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Authentication;

/// <summary>
/// Configurações do JWT
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
}

/// <summary>
/// Implementação do gerador de tokens JWT
/// </summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenGenerator> _logger;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenGenerator> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            var tokenHandler = new JwtSecurityTokenHandler();
            var expirationTime = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

            // Garantir que todas as claims necessárias estão presentes
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),  // Use JWT standard claim names
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // Keep original ClaimTypes for compatibility
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // Todos os usuários têm pelo menos a role Player
                new Claim(ClaimTypes.Role, "Player"),
            };

            // Se o usuário for administrador, adiciona a role "Admin" às claims
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar token JWT");
            throw new InvalidOperationException("Falha ao gerar token de autenticação", ex);
        }
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

            // Validamos o token e obtemos o token validado
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            // Extraímos o claim específico do ID do usuário
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao extrair ID do usuário do token: {Message}", ex.Message);
            return null;
        }
    }

    public string GetExpirationReason(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "Token inválido";

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Primeiro verifica se o token tem formato válido
            if (!tokenHandler.CanReadToken(token))
                return "Token inválido";
                
            // Lê o token sem validação para verificar a data de expiração
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Verifica se o token está expirado
            if (jwtToken.ValidTo < DateTime.UtcNow)
                return "Token expirado";
                
            // Tenta validar o token completo para outros problemas
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // Não valida expiração aqui pois já verificamos acima
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            
            // Se chegou aqui, o token é válido
            return string.Empty;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return "Assinatura do token inválida";
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return "Emissor do token inválido";
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return "Audiência do token inválida";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao validar token: {Message}", ex.Message);
            return "Token inválido";
        }
    }
}

