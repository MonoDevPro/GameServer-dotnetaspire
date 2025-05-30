using System.Diagnostics;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior de performance para comandos usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
public class PerformancePipelineBehavior<TCommand> : IPipelineBehavior<TCommand, Result>
    where TCommand : ICommand
{
    private readonly ILogger<PerformancePipelineBehavior<TCommand>> _logger;
    private readonly long _slowExecutionThresholdMs;

    public PerformancePipelineBehavior(
        ILogger<PerformancePipelineBehavior<TCommand>> logger,
        long slowExecutionThresholdMs = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _slowExecutionThresholdMs = slowExecutionThresholdMs;
    }

    public async Task<Result> Handle(
        TCommand request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Iniciando monitoramento de performance para comando {CommandName}", commandName);

        try
        {
            var result = await next();

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            LogPerformance(commandName, elapsed, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Comando {CommandName} falhou após {ElapsedMs}ms com exceção",
                commandName, elapsed);
            throw;
        }
    }

    private void LogPerformance(string commandName, long elapsedMs, bool isSuccess)
    {
        var statusText = isSuccess ? "sucesso" : "falha";

        if (elapsedMs > _slowExecutionThresholdMs)
        {
            _logger.LogWarning("⚠️ Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms (LENTO - acima do limite de {ThresholdMs}ms)",
                commandName, statusText, elapsedMs, _slowExecutionThresholdMs);
        }
        else if (elapsedMs > _slowExecutionThresholdMs / 2)
        {
            _logger.LogInformation("Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms",
                commandName, statusText, elapsedMs);
        }
        else
        {
            _logger.LogDebug("Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms",
                commandName, statusText, elapsedMs);
        }
    }
}

/// <summary>
/// Behavior de performance para comandos com retorno usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public class PerformancePipelineBehavior<TCommand, TResult> : IPipelineBehavior<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
    private readonly ILogger<PerformancePipelineBehavior<TCommand, TResult>> _logger;
    private readonly long _slowExecutionThresholdMs;

    public PerformancePipelineBehavior(
        ILogger<PerformancePipelineBehavior<TCommand, TResult>> logger,
        long slowExecutionThresholdMs = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _slowExecutionThresholdMs = slowExecutionThresholdMs;
    }

    public async Task<Result<TResult>> Handle(
        TCommand request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Iniciando monitoramento de performance para comando {CommandName}", commandName);

        try
        {
            var result = await next();

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            LogPerformance(commandName, elapsed, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Comando {CommandName} falhou após {ElapsedMs}ms com exceção",
                commandName, elapsed);
            throw;
        }
    }

    private void LogPerformance(string commandName, long elapsedMs, bool isSuccess)
    {
        var statusText = isSuccess ? "sucesso" : "falha";

        if (elapsedMs > _slowExecutionThresholdMs)
        {
            _logger.LogWarning("⚠️ Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms (LENTO - acima do limite de {ThresholdMs}ms)",
                commandName, statusText, elapsedMs, _slowExecutionThresholdMs);
        }
        else if (elapsedMs > _slowExecutionThresholdMs / 2)
        {
            _logger.LogInformation("Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms",
                commandName, statusText, elapsedMs);
        }
        else
        {
            _logger.LogDebug("Comando {CommandName} executado com {StatusText} em {ElapsedMs}ms",
                commandName, statusText, elapsedMs);
        }
    }
}

/// <summary>
/// Behavior de performance para queries usando o sistema de pipeline
/// </summary>
/// <typeparam name="TQuery">Tipo da query</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public class QueryPerformancePipelineBehavior<TQuery, TResult> : IPipelineBehavior<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>
{
    private readonly ILogger<QueryPerformancePipelineBehavior<TQuery, TResult>> _logger;
    private readonly long _slowExecutionThresholdMs;

    public QueryPerformancePipelineBehavior(
        ILogger<QueryPerformancePipelineBehavior<TQuery, TResult>> logger,
        long slowExecutionThresholdMs = 500) // Queries devem ser mais rápidas
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _slowExecutionThresholdMs = slowExecutionThresholdMs;
    }

    public async Task<Result<TResult>> Handle(
        TQuery request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Iniciando monitoramento de performance para query {QueryName}", queryName);

        try
        {
            var result = await next();

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            LogPerformance(queryName, elapsed, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Query {QueryName} falhou após {ElapsedMs}ms com exceção",
                queryName, elapsed);
            throw;
        }
    }

    private void LogPerformance(string queryName, long elapsedMs, bool isSuccess)
    {
        var statusText = isSuccess ? "sucesso" : "falha";

        if (elapsedMs > _slowExecutionThresholdMs)
        {
            _logger.LogWarning("⚠️ Query {QueryName} executada com {StatusText} em {ElapsedMs}ms (LENTA - acima do limite de {ThresholdMs}ms)",
                queryName, statusText, elapsedMs, _slowExecutionThresholdMs);
        }
        else if (elapsedMs > _slowExecutionThresholdMs / 2)
        {
            _logger.LogInformation("Query {QueryName} executada com {StatusText} em {ElapsedMs}ms",
                queryName, statusText, elapsedMs);
        }
        else
        {
            _logger.LogDebug("Query {QueryName} executada com {StatusText} em {ElapsedMs}ms",
                queryName, statusText, elapsedMs);
        }
    }
}
