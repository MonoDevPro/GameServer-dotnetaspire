using System.Diagnostics;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior para logging de performance de comandos
/// </summary>
public class PerformanceBehavior<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _handler;
    private readonly ILogger<PerformanceBehavior<TCommand>> _logger;

    public PerformanceBehavior(
        ICommandHandler<TCommand> handler,
        ILogger<PerformanceBehavior<TCommand>> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _handler.HandleAsync(command, cancellationToken);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (elapsed > 1000) // Log se demorar mais que 1 segundo
            {
                _logger.LogWarning("Comando {CommandName} demorou {ElapsedMs}ms para executar",
                    commandName, elapsed);
            }
            else
            {
                _logger.LogDebug("Comando {CommandName} executado em {ElapsedMs}ms",
                    commandName, elapsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao executar comando {CommandName} após {ElapsedMs}ms",
                commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Behavior para logging de performance de comandos com retorno
/// </summary>
public class PerformanceBehavior<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _handler;
    private readonly ILogger<PerformanceBehavior<TCommand, TResult>> _logger;

    public PerformanceBehavior(
        ICommandHandler<TCommand, TResult> handler,
        ILogger<PerformanceBehavior<TCommand, TResult>> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _handler.HandleAsync(command, cancellationToken);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (elapsed > 1000) // Log se demorar mais que 1 segundo
            {
                _logger.LogWarning("Comando {CommandName} demorou {ElapsedMs}ms para executar",
                    commandName, elapsed);
            }
            else
            {
                _logger.LogDebug("Comando {CommandName} executado em {ElapsedMs}ms",
                    commandName, elapsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao executar comando {CommandName} após {ElapsedMs}ms",
                commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Behavior para logging de performance de queries
/// </summary>
public class QueryPerformanceBehavior<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _handler;
    private readonly ILogger<QueryPerformanceBehavior<TQuery, TResult>> _logger;

    public QueryPerformanceBehavior(
        IQueryHandler<TQuery, TResult> handler,
        ILogger<QueryPerformanceBehavior<TQuery, TResult>> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var queryName = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _handler.HandleAsync(query, cancellationToken);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (elapsed > 500) // Log se demorar mais que 500ms (queries devem ser mais rápidas)
            {
                _logger.LogWarning("Query {QueryName} demorou {ElapsedMs}ms para executar",
                    queryName, elapsed);
            }
            else
            {
                _logger.LogDebug("Query {QueryName} executada em {ElapsedMs}ms",
                    queryName, elapsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao executar query {QueryName} após {ElapsedMs}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
