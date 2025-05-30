using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Commands;

public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandBus> _logger;
    private readonly CommandPipeline _pipeline;

    public CommandBus(IServiceProvider serviceProvider, ILogger<CommandBus> logger, CommandPipeline pipeline)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public async Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var commandName = typeof(TCommand).Name;
        using var scope = _logger.BeginScope("Executing command {CommandName}", commandName);

        _logger.LogInformation("Iniciando execução do comando {CommandName}", commandName);

        try
        {
            // Usar pipeline para executar o comando
            return await _pipeline.Execute<TCommand>(command, async (cmd, token) =>
            {
                var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
                if (handler == null)
                {
                    var errorMessage = $"Nenhum handler encontrado para o comando {commandName}";
                    _logger.LogError(errorMessage);
                    return Result.Failure(errorMessage);
                }

                return await handler.HandleAsync(cmd, token);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Comando {CommandName} foi cancelado", commandName);
            return Result.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar comando {CommandName}", commandName);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }

    public async Task<Result<TResult>> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var commandName = typeof(TCommand).Name;
        using var scope = _logger.BeginScope("Executing command {CommandName} with result {ResultType}", commandName, typeof(TResult).Name);

        _logger.LogInformation("Iniciando execução do comando {CommandName}", commandName);

        try
        {
            // Usar pipeline para executar o comando
            return await _pipeline.Execute<TCommand, TResult>(command, async (cmd, token) =>
            {
                var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
                if (handler == null)
                {
                    var errorMessage = $"Nenhum handler encontrado para o comando {commandName}";
                    _logger.LogError(errorMessage);
                    return Result<TResult>.Failure(errorMessage);
                }

                return await handler.HandleAsync(cmd, token);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Comando {CommandName} foi cancelado", commandName);
            return Result<TResult>.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar comando {CommandName}", commandName);
            return Result<TResult>.Failure($"Erro interno: {ex.Message}");
        }
    }
}
