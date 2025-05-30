using System.Security.Claims;
using GameServer.AuthServer.Health;
using GameServer.AuthServer.Models;
using GameServer.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GameServer.AuthServer;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire components
        builder.AddServiceDefaults();

        // Add Razor Pages
        builder.Services.AddRazorPages();

        // Configure the database context
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Use the connection string named "authdb" (will be injected by Aspire)
            var connectionString = builder.Configuration.GetConnectionString("authdb");
            if (string.IsNullOrEmpty(connectionString))
                // Fallback for local development
                options.UseInMemoryDatabase("authdb-local");
            else
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(3));

            options.UseOpenIddict();
        });

        // Add ASP.NET Core Identity
        builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
        {
            // Configure Identity options
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register custom services
        builder.Services.AddScoped<GameServer.AuthServer.Services.IUserAuthenticationTypeService, GameServer.AuthServer.Services.UserAuthenticationTypeService>();
        builder.Services.AddScoped<GameServer.AuthServer.Services.IPasswordAuditService, GameServer.AuthServer.Services.PasswordAuditService>();

        // Configure external authentication providers
        builder.Services.AddAuthentication()
            .AddGitHub(options =>
            {
                options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "";
                options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "";
                options.CallbackPath = "/signin-github";

                // Request additional scopes
                options.Scope.Add("user:email");

                // Save tokens to be able to call GitHub API later
                options.SaveTokens = true;

                // Map claims from GitHub to standard claims
                options.ClaimActions.MapJsonKey("urn:github:login", "login");
                options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
                options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                // Map GitHub name to ClaimTypes.Name and ClaimTypes.GivenName
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "name");

                // Map GitHub login as backup for name
                options.ClaimActions.MapJsonKey("urn:github:username", "login");
            });

        // Configure cookie authentication
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
        });

        // Configure Quartz.NET for OpenIddict
        builder.Services.AddQuartz(options =>
        {
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });

        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        // Configure OpenIddict
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
                options.UseQuartz();
            })
            .AddServer(options =>
            {
                // Configure endpoints
                options.SetAuthorizationEndpointUris(builder.Configuration["OpenIddict:Endpoints:Authorization"]!)
                       .SetTokenEndpointUris(builder.Configuration["OpenIddict:Endpoints:Token"]!)
                       .SetIntrospectionEndpointUris(builder.Configuration["OpenIddict:Endpoints:Introspection"]!)
                       .SetUserInfoEndpointUris(builder.Configuration["OpenIddict:Endpoints:Userinfo"]!)
                       .SetEndSessionEndpointUris(builder.Configuration["OpenIddict:Endpoints:Logout"]!);

                // Configure flows
                options.AllowAuthorizationCodeFlow()
                       .AllowImplicitFlow()
                       .AllowHybridFlow()
                       .AllowRefreshTokenFlow();

                // Configure claims and scopes
                options.RegisterClaims(builder.Configuration.GetSection("OpenIddict:Claims").Get<string[]>()!);
                options.RegisterScopes(builder.Configuration.GetSection("OpenIddict:Scopes").Get<string[]>()!);

                // Configure signing keys
                options.AddEphemeralEncryptionKey()
                       .AddEphemeralSigningKey();

                // Configure ASP.NET Core host
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.EnableAuthorizationEntryValidation();
            });

        // Add health checks with proper timeout configuration
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("authdb",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["ready"]);

        // Register the seeding service
        builder.Services.AddHostedService<DatabaseSeederService<ApplicationDbContext>>();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        // Map default endpoints (health checks)
        app.MapDefaultEndpoints();

        app.Run();
    }
}