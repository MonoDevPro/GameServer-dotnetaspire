using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior para validação automática de comandos
/// </summary>
public class ValidationBehavior<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _handler;
    private readonly IEnumerable<IValidator<TCommand>> _validators;
    private readonly ILogger<ValidationBehavior<TCommand>> _logger;

    public ValidationBehavior(
        ICommandHandler<TCommand> handler,
        IEnumerable<IValidator<TCommand>> validators,
        ILogger<ValidationBehavior<TCommand>> logger)
    {
        _handler = handler;
        _validators = validators;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;

        if (_validators.Any())
        {
            _logger.LogDebug("Validando comando {CommandName}", commandName);

            var context = new ValidationContext<TCommand>(command);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.Message)
                .ToList();

            if (failures.Count > 0)
            {
                _logger.LogWarning("Comando {CommandName} falhou na validação: {Errors}",
                    commandName, string.Join("; ", failures));
                return Result.Failure(failures);
            }
        }

        return await _handler.HandleAsync(command, cancellationToken);
    }
}

/// <summary>
/// Behavior para validação automática de comandos com retorno
/// </summary>
public class ValidationBehavior<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _handler;
    private readonly IEnumerable<IValidator<TCommand>> _validators;
    private readonly ILogger<ValidationBehavior<TCommand, TResult>> _logger;

    public ValidationBehavior(
        ICommandHandler<TCommand, TResult> handler,
        IEnumerable<IValidator<TCommand>> validators,
        ILogger<ValidationBehavior<TCommand, TResult>> logger)
    {
        _handler = handler;
        _validators = validators;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;

        if (_validators.Any())
        {
            _logger.LogDebug("Validando comando {CommandName}", commandName);

            var context = new ValidationContext<TCommand>(command);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.Message)
                .ToList();

            if (failures.Count > 0)
            {
                _logger.LogWarning("Comando {CommandName} falhou na validação: {Errors}",
                    commandName, string.Join("; ", failures));
                return Result<TResult>.Failure(failures);
            }
        }

        return await _handler.HandleAsync(command, cancellationToken);
    }
}
