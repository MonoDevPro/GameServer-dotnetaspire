using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace GameServer.AuthService.Service.Infrastructure.Security;

/// <summary>
/// Interface para serviço de hash de senhas
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera um hash da senha fornecida
    /// </summary>
    /// <param name="password">Senha em texto claro</param>
    /// <returns>Hash da senha</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifica se uma senha corresponde ao hash fornecido
    /// </summary>
    /// <param name="password">Senha em texto claro</param>
    /// <param name="hash">Hash armazenado</param>
    /// <returns>Verdadeiro se a senha corresponde ao hash, falso caso contrário</returns>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Implementação do serviço de hash de senhas usando PBKDF2
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int IterationCount = 10000;
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const char Delimiter = ':';
    
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        // Gerar um salt aleatório
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        
        // Derivar um hash usando PBKDF2 com HMAC-SHA256
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: IterationCount,
            numBytesRequested: KeySize);
        
        // Formato: iterações:salt:hash
        return $"{IterationCount}{Delimiter}{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}";
    }
    
    /// <inheritdoc />
    public bool VerifyPassword(string password, string hashString)
    {
        // Extrair as partes do hash
        var parts = hashString.Split(Delimiter);
        if (parts.Length != 3)
        {
            return false;
        }
        
        // Extrair os parâmetros
        if (!int.TryParse(parts[0], out int iterations))
        {
            return false;
        }
        
        byte[] salt;
        byte[] hash;
        
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            hash = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }
        
        // Recalcular o hash com a mesma senha e salt
        byte[] computedHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: hash.Length);
        
        // Comparar os hashes
        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }
}
