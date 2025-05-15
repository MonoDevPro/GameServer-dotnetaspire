namespace GameServer.AuthService.Application.Models;

public record RegisterRequest(string Username, string Email, string Password);

public record LoginRequest(string UsernameOrEmail, string Password);

public record AuthResponse(bool Success, string Token, string Message);

public record UpdateAccountRequest(string? Email = null, string? CurrentPassword = null, string? NewPassword = null);

public record AccountResponse(
    Guid Id, 
    string Username, 
    string Email, 
    bool IsActive, 
    DateTime CreatedAt, 
    DateTime? LastLoginAt, 
    bool IsBanned,
    DateTime? BannedUntil);

public record BanRequest(DateTime Until, string Reason);
public record ErrorResponse(string Message, string? Details = null);