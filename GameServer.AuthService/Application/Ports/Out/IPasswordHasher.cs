using System;

namespace GameServer.AuthService.Application.Ports.Out;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure hashing algorithm.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies if the provided password matches the hashed password.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the passwords match; otherwise, false.</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
