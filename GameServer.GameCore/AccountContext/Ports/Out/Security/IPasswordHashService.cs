namespace GameServer.GameCore.AccountContext.Ports.Out.Security;

public interface IPasswordHashService
{
    /// <summary>
    /// Hash a password
    /// </summary>
    /// <param name="password">Password to hash</param>
    /// <returns>Hashed password</returns>
    string Hash(string password);

    /// <summary>
    /// Verify a password
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>True if the password is valid, false otherwise</returns>
    bool Verify(string password, string hashedPassword);
}