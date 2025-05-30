namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Resultado de uma validação
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationResult()
    {
        IsValid = true;
        Errors = Array.Empty<ValidationError>();
    }

    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        var errorList = errors?.Where(e => e != null)?.ToList() ?? new List<ValidationError>();
        IsValid = errorList.Count == 0;
        Errors = errorList.AsReadOnly();
    }

    public ValidationResult(params ValidationError[] errors) : this((IEnumerable<ValidationError>)errors)
    {
    }

    // Backward compatibility with string errors
    public ValidationResult(IEnumerable<string> errors)
    {
        var errorList = errors?.Where(e => !string.IsNullOrWhiteSpace(e))
            ?.Select(e => new ValidationError(e))?.ToList() ?? new List<ValidationError>();
        IsValid = errorList.Count == 0;
        Errors = errorList.AsReadOnly();
    }

    public ValidationResult(params string[] errors) : this((IEnumerable<string>)errors)
    {
    }

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(params ValidationError[] errors) => new(errors);
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new(errors);
    public static ValidationResult Failure(params string[] errors) => new(errors);
    public static ValidationResult Failure(IEnumerable<string> errors) => new(errors);
}
