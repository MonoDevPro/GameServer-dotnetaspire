using System.Security.Claims;
using GameServer.AccountService.Service.Infrastructure.Helper;
using Microsoft.AspNetCore.Authorization;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;

/// <summary>
/// Permission handler for custom authorization implementations
/// </summary>
public class AppPermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity is null)
        {
            return Task.CompletedTask;
        }

        var identity = (ClaimsIdentity)context.User.Identity;
        var claim = ClaimsHelper.GetValue<string>(identity, requirement.PermissionName);

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}