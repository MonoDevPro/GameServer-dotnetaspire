using System.Linq.Expressions;

namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Classe base para validadores que implementa funcionalidades comuns
/// </summary>
/// <typeparam name="T">Tipo do objeto a ser validado</typeparam>
public abstract class BaseValidator<T> : IValidator<T>
{
    private readonly List<Func<T, ValidationResult>> _rules = [];

    /// <summary>
    /// Adiciona uma regra de validação
    /// </summary>
    protected void AddRule(Func<T, ValidationResult> rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    /// Adiciona uma regra simples com condição e mensagem de erro
    /// </summary>
    protected void AddRule(Func<T, bool> condition, string errorMessage, string? propertyName = null)
    {
        _rules.Add(instance =>
        {
            if (!condition(instance))
            {
                return ValidationResult.Failure(new ValidationError(errorMessage, propertyName));
            }
            return ValidationResult.Success();
        });
    }

    /// <summary>
    /// Adiciona uma regra para uma propriedade específica
    /// </summary>
    protected void RuleFor<TProperty>(Expression<Func<T, TProperty>> expression, Func<TProperty, bool> condition, string errorMessage)
    {
        var propertyName = GetPropertyName(expression);
        var propertySelector = expression.Compile();

        _rules.Add(instance =>
        {
            var propertyValue = propertySelector(instance);
            if (!condition(propertyValue))
            {
                return ValidationResult.Failure(new ValidationError(errorMessage, propertyName));
            }
            return ValidationResult.Success();
        });
    }

    /// <summary>
    /// Adiciona uma regra para verificar se uma propriedade não é nula
    /// </summary>
    protected void RuleForNotNull<TProperty>(Expression<Func<T, TProperty>> expression, string? customMessage = null)
    {
        var propertyName = GetPropertyName(expression);
        var message = customMessage ?? $"{propertyName} is required";

        RuleFor(expression, value => value != null, message);
    }

    /// <summary>
    /// Adiciona uma regra para verificar se uma string não é nula ou vazia
    /// </summary>
    protected void RuleForNotEmpty(Expression<Func<T, string?>> expression, string? customMessage = null)
    {
        var propertyName = GetPropertyName(expression);
        var message = customMessage ?? $"{propertyName} cannot be empty";

        RuleFor(expression, value => !string.IsNullOrWhiteSpace(value), message);
    }

    /// <summary>
    /// Adiciona uma regra para verificar o comprimento mínimo de uma string
    /// </summary>
    protected void RuleForMinLength(Expression<Func<T, string?>> expression, int minLength, string? customMessage = null)
    {
        var propertyName = GetPropertyName(expression);
        var message = customMessage ?? $"{propertyName} must be at least {minLength} characters long";

        RuleFor(expression, value => value?.Length >= minLength, message);
    }

    /// <summary>
    /// Adiciona uma regra para verificar o comprimento máximo de uma string
    /// </summary>
    protected void RuleForMaxLength(Expression<Func<T, string?>> expression, int maxLength, string? customMessage = null)
    {
        var propertyName = GetPropertyName(expression);
        var message = customMessage ?? $"{propertyName} must be at most {maxLength} characters long";

        RuleFor(expression, value => value?.Length <= maxLength, message);
    }

    /// <summary>
    /// Adiciona uma regra para verificar se um valor está dentro de um range
    /// </summary>
    protected void RuleForRange<TProperty>(Expression<Func<T, TProperty>> expression, TProperty min, TProperty max, string? customMessage = null)
        where TProperty : IComparable<TProperty>
    {
        var propertyName = GetPropertyName(expression);
        var message = customMessage ?? $"{propertyName} must be between {min} and {max}";

        RuleFor(expression, value => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0, message);
    }

    public virtual ValidationResult Validate(T instance)
    {
        if (instance == null)
            return ValidationResult.Failure(new ValidationError("Instance cannot be null"));

        var errors = new List<ValidationError>();

        foreach (var rule in _rules)
        {
            var result = rule(instance);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    public virtual ValidationResult Validate(ValidationContext<T> context)
    {
        return Validate(context.Instance);
    }

    public virtual Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken = default)
    {
        return ValidateAsync(context.Instance, cancellationToken);
    }

    /// <summary>
    /// Obtém o nome da propriedade a partir de uma expressão
    /// </summary>
    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        return expression.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpression2 } => memberExpression2.Member.Name,
            _ => throw new ArgumentException("Invalid property expression", nameof(expression))
        };
    }
}
