using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Factories;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using GameServer.AccountService.Service.Definitions.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public static class IdentityExtension
{
    public static WebApplicationBuilder ConfigureIdentity(this WebApplicationBuilder builder)
    {
        // Configurar Identity
        builder.Services.AddIdentity<ApplicationUser, ApplicationUserRole>(options =>
            {
                // var identityConfig = builder.Configuration.GetSection("IdentityConfig");

                // // Aplicar configurações do appsettings.json
                // options.SignIn.RequireConfirmedEmail = identityConfig.GetValue<bool>("RequireConfirmedEmail");
                // options.SignIn.RequireConfirmedAccount = identityConfig.GetValue<bool>("RequireConfirmedAccount");

                // // Configurações de senha
                // var passwordConfig = identityConfig.GetSection("PasswordRequirements");
                // options.Password.RequireDigit = passwordConfig.GetValue<bool>("RequireDigit");
                // options.Password.RequireUppercase = passwordConfig.GetValue<bool>("RequireUppercase");
                // options.Password.RequireLowercase = passwordConfig.GetValue<bool>("RequireLowercase");
                // options.Password.RequireNonAlphanumeric = passwordConfig.GetValue<bool>("RequireNonAlphanumeric");
                // options.Password.RequiredLength = passwordConfig.GetValue<int>("RequiredLength");

                // // Configurações de bloqueio
                // var lockoutConfig = identityConfig.GetSection("LockoutSettings");
                // options.Lockout.MaxFailedAccessAttempts = lockoutConfig.GetValue<int>("MaxFailedAttempts");
                // options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                //     lockoutConfig.GetValue<int>("DefaultLockoutTimeSpanMinutes"));
            })
            .AddClaimsPrincipalFactory<UserClaimsPrincipalFactory>()
            .AddEntityFrameworkStores<AccountDbContext>()
            .AddDefaultTokenProviders();

        return builder;
    }

    public static WebApplication UseApplicationIdentity(this WebApplication app)
    {
        // registering UserIdentity helper as singleton
        UserIdentity.Instance.Configure(app.Services.GetService<IHttpContextAccessor>()!);

        return app;
    }
}