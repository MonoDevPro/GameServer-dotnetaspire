using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior de logging para comandos usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
public class LoggingPipelineBehavior<TCommand> : IPipelineBehavior<TCommand, Result>
    where TCommand : ICommand
{
    private readonly ILogger<LoggingPipelineBehavior<TCommand>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TCommand>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        TCommand request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;

        using var scope = _logger.BeginScope("Processing command {CommandName}", commandName);

        _logger.LogInformation("🚀 Iniciando execução do comando {CommandName}", commandName);

        try
        {
            var result = await next();

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Comando {CommandName} executado com sucesso", commandName);
            }
            else
            {
                _logger.LogWarning("❌ Comando {CommandName} falhou: {Errors}",
                    commandName, string.Join("; ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante execução do comando {CommandName}", commandName);
            throw;
        }
    }
}

/// <summary>
/// Behavior de logging para comandos com retorno usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public class LoggingPipelineBehavior<TCommand, TResult> : IPipelineBehavior<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
    private readonly ILogger<LoggingPipelineBehavior<TCommand, TResult>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TCommand, TResult>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TResult>> Handle(
        TCommand request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var resultType = typeof(TResult).Name;

        using var scope = _logger.BeginScope("Processing command {CommandName} -> {ResultType}", commandName, resultType);

        _logger.LogInformation("🚀 Iniciando execução do comando {CommandName} -> {ResultType}", commandName, resultType);

        try
        {
            var result = await next();

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Comando {CommandName} executado com sucesso, retornando {ResultType}",
                    commandName, resultType);
            }
            else
            {
                _logger.LogWarning("❌ Comando {CommandName} falhou: {Errors}",
                    commandName, string.Join("; ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante execução do comando {CommandName}", commandName);
            throw;
        }
    }
}

/// <summary>
/// Behavior de logging para queries usando o sistema de pipeline
/// </summary>
/// <typeparam name="TQuery">Tipo da query</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public class QueryLoggingPipelineBehavior<TQuery, TResult> : IPipelineBehavior<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>
{
    private readonly ILogger<QueryLoggingPipelineBehavior<TQuery, TResult>> _logger;

    public QueryLoggingPipelineBehavior(ILogger<QueryLoggingPipelineBehavior<TQuery, TResult>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TResult>> Handle(
        TQuery request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TQuery).Name;
        var resultType = typeof(TResult).Name;

        using var scope = _logger.BeginScope("Processing query {QueryName} -> {ResultType}", queryName, resultType);

        _logger.LogInformation("🔍 Iniciando execução da query {QueryName} -> {ResultType}", queryName, resultType);

        try
        {
            var result = await next();

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Query {QueryName} executada com sucesso, retornando {ResultType}",
                    queryName, resultType);
            }
            else
            {
                _logger.LogWarning("❌ Query {QueryName} falhou: {Errors}",
                    queryName, string.Join("; ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante execução da query {QueryName}", queryName);
            throw;
        }
    }
}
