using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AuthService.Service.Domain.Entities
{
    /// <summary>
    /// Application store for user
    /// </summary>
    public class ApplicationUserStore : UserStore<ApplicationUser, ApplicationUserRole, AuthDbContext, Guid>
    {
        public ApplicationUserStore(AuthDbContext context, IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {

        }

        /// <inheritdoc />
        public override Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = new CancellationToken())
            => Users
                .Include(x => x.ApplicationUserProfile).ThenInclude(x => x!.Permissions)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken: cancellationToken);
    }
}