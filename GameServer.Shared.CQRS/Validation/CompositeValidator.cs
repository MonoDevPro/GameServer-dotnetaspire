namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Validador composto que executa múltiplos validadores
/// </summary>
/// <typeparam name="T">Tipo do objeto a ser validado</typeparam>
public class CompositeValidator<T> : IValidator<T>
{
    private readonly IList<IValidator<T>> _validators;

    public CompositeValidator(params IValidator<T>[] validators)
    {
        _validators = validators?.ToList() ?? new List<IValidator<T>>();
    }

    public CompositeValidator(IEnumerable<IValidator<T>> validators)
    {
        _validators = validators?.ToList() ?? new List<IValidator<T>>();
    }

    /// <summary>
    /// Adiciona um validador ao composto
    /// </summary>
    public CompositeValidator<T> AddValidator(IValidator<T> validator)
    {
        if (validator != null)
        {
            _validators.Add(validator);
        }
        return this;
    }

    public ValidationResult Validate(T instance)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = validator.Validate(instance);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public ValidationResult Validate(ValidationContext<T> context)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = validator.Validate(context);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public async Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
