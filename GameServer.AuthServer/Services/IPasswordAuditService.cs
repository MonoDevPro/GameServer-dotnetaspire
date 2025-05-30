using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthServer.Services;

public interface IPasswordAuditService
{
    Task LogPasswordChangeAsync(IdentityUser<Guid> user, bool success, string? reason = null, string? ipAddress = null, string? userAgent = null);
    Task LogPasswordChangeAttemptAsync(IdentityUser<Guid> user, bool success, string? ipAddress = null, string? userAgent = null);
}
