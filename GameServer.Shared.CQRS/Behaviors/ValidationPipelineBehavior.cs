using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior de validação para comandos usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
public class ValidationPipelineBehavior<TCommand> : IPipelineBehavior<TCommand, Result>
    where TCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationPipelineBehavior<TCommand>> _logger;

    public ValidationPipelineBehavior(
        IServiceProvider serviceProvider,
        ILogger<ValidationPipelineBehavior<TCommand>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        TCommand request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        _logger.LogDebug("Iniciando validação para comando {CommandName}", commandName);

        try
        {
            // Obter todos os validators para este comando
            var validators = _serviceProvider.GetServices<IValidator<TCommand>>().ToList();

            if (!validators.Any())
            {
                _logger.LogDebug("Nenhum validator encontrado para {CommandName}, prosseguindo", commandName);
                return await next();
            }

            _logger.LogDebug("Executando {ValidatorCount} validators para {CommandName}",
                validators.Count(), commandName);

            // Executar todas as validações
            var context = new ValidationContext<TCommand>(request);
            var validationTasks = validators.Select(v => v.ValidateAsync(context, cancellationToken));
            var validationResults = await Task.WhenAll(validationTasks);

            // Coletar todos os erros
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.Message)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Comando {CommandName} falhou na validação: {Errors}",
                    commandName, string.Join("; ", failures));
                return Result.Failure(failures);
            }

            _logger.LogDebug("Validação concluída com sucesso para {CommandName}", commandName);
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante validação do comando {CommandName}", commandName);
            return Result.Failure($"Erro na validação: {ex.Message}");
        }
    }
}

/// <summary>
/// Behavior de validação para comandos com retorno usando o sistema de pipeline
/// </summary>
/// <typeparam name="TCommand">Tipo do comando</typeparam>
/// <typeparam name="TResult">Tipo do resultado</typeparam>
public class ValidationPipelineBehavior<TCommand, TResult> : IPipelineBehavior<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationPipelineBehavior<TCommand, TResult>> _logger;

    public ValidationPipelineBehavior(
        IServiceProvider serviceProvider,
        ILogger<ValidationPipelineBehavior<TCommand, TResult>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TResult>> Handle(
        TCommand request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        _logger.LogDebug("Iniciando validação para comando {CommandName}", commandName);

        try
        {
            // Obter todos os validators para este comando
            var validators = _serviceProvider.GetServices<IValidator<TCommand>>().ToList();

            if (!validators.Any())
            {
                _logger.LogDebug("Nenhum validator encontrado para {CommandName}, prosseguindo", commandName);
                return await next();
            }

            _logger.LogDebug("Executando {ValidatorCount} validators para {CommandName}",
                validators.Count(), commandName);

            // Executar todas as validações
            var context = new ValidationContext<TCommand>(request);
            var validationTasks = validators.Select(v => v.ValidateAsync(context, cancellationToken));
            var validationResults = await Task.WhenAll(validationTasks);

            // Coletar todos os erros
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.Message)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Comando {CommandName} falhou na validação: {Errors}",
                    commandName, string.Join("; ", failures));
                return Result<TResult>.Failure(failures);
            }

            _logger.LogDebug("Validação concluída com sucesso para {CommandName}", commandName);
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante validação do comando {CommandName}", commandName);
            return Result<TResult>.Failure($"Erro na validação: {ex.Message}");
        }
    }
}
