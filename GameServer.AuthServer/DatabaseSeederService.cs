using System.Security.Claims;
using GameServer.AuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GameServer.AuthServer;

public class DatabaseSeederService<TContext> : IHostedService
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _provider;
    private readonly ILogger<DatabaseSeederService<TContext>> _logger;

    public DatabaseSeederService(
        TContext context,
        IConfiguration configuration,
        IServiceProvider provider,
        ILogger<DatabaseSeederService<TContext>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration;
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Add initial delay to allow other services to start first
        await Task.Delay(3000, cancellationToken);

        try
        {
            
            var scope = _provider.CreateAsyncScope();
            
            // Check if using in-memory database
            var connectionString = _configuration.GetConnectionString("authdb");
            var isInMemory = string.IsNullOrEmpty(connectionString);

            if (isInMemory)
            {
                _logger.LogInformation("Using in-memory database for development");
                await _context.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("Attempting to connect to PostgreSQL database");

                // Wait for database to be available with retry logic
                var retryCount = 0;
                const int maxRetries = 5; // Reduced from 10 to 5

                while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        
                        // Use shorter timeout for connection test
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(3));

                        if (await _context.Database.CanConnectAsync(cts.Token))
                        {
                            _logger.LogInformation("Database connection successful");
                            break;
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Database seeding cancelled");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Database connection attempt {RetryCount}/{MaxRetries} failed: {Error}",
                            retryCount + 1, maxRetries, ex.Message);
                    }

                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(3000, cancellationToken); // Reduced to 3 seconds between retries
                    }
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning("Could not connect to database after {MaxRetries} attempts. Service will continue with limited functionality.", maxRetries);
                    return;
                }

                try
                {
                    // Apply pending migrations
                    await _context.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migrations applied successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not apply migrations, using EnsureCreated instead");
                    await _context.Database.EnsureCreatedAsync(cancellationToken);
                }
            }

            // Seed OpenIddict applications
            await SeedOpenIddictApplicationsAsync(scope, cancellationToken);

            // Seed default users if needed
            await SeedDefaultUsersAsync(scope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Database seeding was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed. Service will continue with limited functionality.");
        }
    }

    private async Task SeedOpenIddictApplicationsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var descriptors = _configuration.GetSection("OpenIddict:Clients").Get<OpenIddictApplicationDescriptor[]>();
        if (descriptors is not { Length: > 0 })
        {
            _logger.LogWarning("No client applications found in configuration");
            return;
        }

        foreach (var descriptor in descriptors)
        {
            if (await manager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is not null)
            {
                continue;
            }

            await manager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Created OpenIddict application: {ClientId}", descriptor.ClientId);
        }
    }

    private async Task SeedDefaultUsersAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

        // Create default admin user if it doesn't exist
        const string adminEmail = "admin@gameserver.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                // Add default claims
                await userManager.AddClaimsAsync(adminUser, new[]
                {
                    new Claim(Claims.Name, "System Administrator"),
                    new Claim(Claims.GivenName, "System"),
                    new Claim(Claims.FamilyName, "Administrator"),
                    new Claim(Claims.Email, adminEmail),
                    new Claim(Claims.Role, "Administrator")
                });

                _logger.LogInformation("Created default admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create default admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
