using System.Security.Cryptography;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Security;

/// <summary>
/// Implementação do serviço de hash de senhas usando PBKDF2
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHashService
{
    private const int IterationCount = 10000;
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const char Delimiter = ':';
    
    /// <inheritdoc />
    public string Hash(string password)
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
    public bool Verify(string password, string hashString)
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
