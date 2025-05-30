using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.Database;

public class DatabaseSeederService<TContext> : IHostedService
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseSeederService<TContext>> _logger;

    public DatabaseSeederService(
        TContext context,
        string connectionString,
        ILogger<DatabaseSeederService<TContext>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Add initial delay to allow other services to start first
        await Task.Delay(3000, cancellationToken);

        try
        {
            // Check if using in-memory database
            var isInMemory = string.IsNullOrEmpty(_connectionString);

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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
