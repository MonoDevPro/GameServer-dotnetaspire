using GameServer.AuthService.Service.Domain.Entities;
using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthService.Service.Definitions.Identity;

public static class IdentityDefinition
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configurar Identity
        builder.Services.AddIdentity<ApplicationUser, ApplicationUserRole>(options =>
            {
                var identityConfig = builder.Configuration.GetSection("IdentityConfig");
            
                // Aplicar configurações do appsettings.json
                options.SignIn.RequireConfirmedEmail = identityConfig.GetValue<bool>("RequireConfirmedEmail");
                options.SignIn.RequireConfirmedAccount = identityConfig.GetValue<bool>("RequireConfirmedAccount");
            
                // Configurações de senha
                var passwordConfig = identityConfig.GetSection("PasswordRequirements");
                options.Password.RequireDigit = passwordConfig.GetValue<bool>("RequireDigit");
                options.Password.RequireUppercase = passwordConfig.GetValue<bool>("RequireUppercase");
                options.Password.RequireLowercase = passwordConfig.GetValue<bool>("RequireLowercase");
                options.Password.RequireNonAlphanumeric = passwordConfig.GetValue<bool>("RequireNonAlphanumeric");
                options.Password.RequiredLength = passwordConfig.GetValue<int>("RequiredLength");
            
                // Configurações de bloqueio
                var lockoutConfig = identityConfig.GetSection("LockoutSettings");
                options.Lockout.MaxFailedAccessAttempts = lockoutConfig.GetValue<int>("MaxFailedAttempts");
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                    lockoutConfig.GetValue<int>("DefaultLockoutTimeSpanMinutes"));
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        
        return builder;
    }
}