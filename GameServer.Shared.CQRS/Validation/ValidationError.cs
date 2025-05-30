namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Representa um erro de validação específico
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Mensagem de erro
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Nome da propriedade onde ocorreu o erro (opcional)
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Código de erro (opcional)
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Valor que causou o erro (opcional)
    /// </summary>
    public object? AttemptedValue { get; }

    public ValidationError(string message, string? propertyName = null, string? errorCode = null, object? attemptedValue = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        PropertyName = propertyName;
        ErrorCode = errorCode;
        AttemptedValue = attemptedValue;
    }

    public override string ToString() => Message;

    public override bool Equals(object? obj)
    {
        if (obj is not ValidationError other) return false;
        return Message == other.Message && PropertyName == other.PropertyName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, PropertyName);
    }
}
