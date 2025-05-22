using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;

/// <summary>
/// Application store for user
/// </summary>
public class ApplicationUserStore : UserStore<ApplicationUser, ApplicationUserRole, AccountDbContext, Guid>
{
    public ApplicationUserStore(AccountDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        
    }

    /// <inheritdoc />
    public Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = new CancellationToken())
        => Users
            .FirstOrDefaultAsync(u => u.Id == userId, 
                cancellationToken: cancellationToken);
}