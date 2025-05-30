using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthServer.Services;

public class PasswordAuditService : IPasswordAuditService
{
    private readonly ILogger<PasswordAuditService> _logger;

    public PasswordAuditService(ILogger<PasswordAuditService> logger)
    {
        _logger = logger;
    }

    public Task LogPasswordChangeAsync(IdentityUser<Guid> user, bool success, string? reason = null, string? ipAddress = null, string? userAgent = null)
    {
        if (success)
        {
            _logger.LogInformation("Password changed successfully for user {UserId} ({Email}) from IP {IpAddress} using {UserAgent}",
                user.Id, user.Email, ipAddress ?? "unknown", userAgent ?? "unknown");
        }
        else
        {
            _logger.LogWarning("Password change failed for user {UserId} ({Email}) from IP {IpAddress}. Reason: {Reason}",
                user.Id, user.Email, ipAddress ?? "unknown", reason ?? "unknown");
        }

        return Task.CompletedTask;
    }

    public Task LogPasswordChangeAttemptAsync(IdentityUser<Guid> user, bool success, string? ipAddress = null, string? userAgent = null)
    {
        if (success)
        {
            _logger.LogInformation("Password change attempt succeeded for user {UserId} ({Email}) from IP {IpAddress}",
                user.Id, user.Email, ipAddress ?? "unknown");
        }
        else
        {
            _logger.LogWarning("Password change attempt failed for user {UserId} ({Email}) from IP {IpAddress} - Invalid current password",
                user.Id, user.Email, ipAddress ?? "unknown");
        }

        return Task.CompletedTask;
    }
}
