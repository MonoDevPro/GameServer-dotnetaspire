namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Interface para validadores
/// </summary>
/// <typeparam name="T">Tipo do objeto a ser validado</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Valida o objeto fornecido
    /// </summary>
    /// <param name="instance">Objeto a ser validado</param>
    /// <returns>Resultado da validação</returns>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Valida o objeto fornecido de forma assíncrona
    /// </summary>
    /// <param name="instance">Objeto a ser validado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da validação</returns>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida o objeto com contexto adicional
    /// </summary>
    /// <param name="context">Contexto de validação</param>
    /// <returns>Resultado da validação</returns>
    ValidationResult Validate(ValidationContext<T> context);

    /// <summary>
    /// Valida o objeto com contexto adicional de forma assíncrona
    /// </summary>
    /// <param name="context">Contexto de validação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da validação</returns>
    Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken = default);
}
