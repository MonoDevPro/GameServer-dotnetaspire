using System.Security.Cryptography;
using System.Text;
using GameServer.AuthService.Application.Ports.Out;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Security;

/// <summary>
/// Classe utilitária para hash e verificação de senhas
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 10000;
    private readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char Delimiter = ':';

    /// <summary>
    /// Gera um hash para uma senha usando PBKDF2 com sal
    /// </summary>
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize
        );

        return string.Join(
            Delimiter,
            Convert.ToHexString(hash),
            Convert.ToHexString(salt),
            Iterations,
            Algorithm
        );
    }

    /// <summary>
    /// Verifica se uma senha corresponde a um hash armazenado
    /// </summary>
    public bool VerifyPassword(string password, string hashString)
    {
        var parts = hashString.Split(Delimiter);
        var hash = Convert.FromHexString(parts[0]);
        var salt = Convert.FromHexString(parts[1]);
        var iterations = int.Parse(parts[2]);
        var algorithm = new HashAlgorithmName(parts[3]);

        var verificationHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            algorithm,
            hash.Length
        );

        return CryptographicOperations.FixedTimeEquals(hash, verificationHash);
    }
}