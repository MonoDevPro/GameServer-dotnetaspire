using GameServer.AuthServer.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameServer.AuthServer.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly IConfiguration _configuration;
    private static DateTime _lastLogTime = DateTime.MinValue;
    private static readonly TimeSpan LogCooldown = TimeSpan.FromMinutes(1);

    public DatabaseHealthCheck(ApplicationDbContext context, ILogger<DatabaseHealthCheck> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("authdb");
            var isInMemory = string.IsNullOrEmpty(connectionString);

            if (isInMemory)
            {
                // For in-memory database, just check if context is available
                return HealthCheckResult.Healthy("In-memory database ready");
            }

            // Use a very short timeout to avoid blocking
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500)); // Reduced to 500ms

            var canConnect = await _context.Database.CanConnectAsync(timeoutCts.Token);

            if (canConnect)
            {
                return HealthCheckResult.Healthy("Database connection successful");
            }
            else
            {
                LogIfCooldownExpired("Database connection failed but service can continue with fallback");
                return HealthCheckResult.Degraded("Database connection failed but service can continue with fallback");
            }
        }
        catch (OperationCanceledException)
        {
            LogIfCooldownExpired("Database connection timeout - using fallback mode");
            return HealthCheckResult.Degraded("Database connection timeout - using fallback mode");
        }
        catch (Exception ex)
        {
            LogIfCooldownExpired($"Database health check failed: {ex.Message}");
            return HealthCheckResult.Degraded("Database unavailable - using fallback mode");
        }
    }

    private void LogIfCooldownExpired(string message)
    {
        var now = DateTime.UtcNow;
        if (now - _lastLogTime > LogCooldown)
        {
            _logger.LogInformation(message);
            _lastLogTime = now;
        }
    }
}
