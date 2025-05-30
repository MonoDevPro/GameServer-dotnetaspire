using System.Text.RegularExpressions;

namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Utilitários para validação comum
/// </summary>
public static class ValidationUtils
{
    /// <summary>
    /// Regex para validação de email
    /// </summary>
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Verifica se um email é válido
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Verifica se uma string contém apenas números
    /// </summary>
    public static bool IsNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
    }

    /// <summary>
    /// Verifica se uma string contém apenas letras
    /// </summary>
    public static bool IsAlphabetic(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetter);
    }

    /// <summary>
    /// Verifica se uma string contém apenas letras e números
    /// </summary>
    public static bool IsAlphaNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Verifica se um GUID é válido e não vazio
    /// </summary>
    public static bool IsValidGuid(Guid? guid)
    {
        return guid.HasValue && guid.Value != Guid.Empty;
    }

    /// <summary>
    /// Verifica se um GUID string é válido
    /// </summary>
    public static bool IsValidGuid(string? guidString)
    {
        return Guid.TryParse(guidString, out var guid) && guid != Guid.Empty;
    }

    /// <summary>
    /// Verifica se uma URL é válida
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Verifica se um valor está dentro de um range
    /// </summary>
    public static bool IsInRange<T>(T value, T min, T max) where T : IComparable<T>
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>
    /// Verifica se uma senha atende aos critérios mínimos de segurança
    /// </summary>
    public static bool IsStrongPassword(string? password, int minLength = 8)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < minLength)
            return false;

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    /// <summary>
    /// Verifica se uma collection não é nula e tem elementos
    /// </summary>
    public static bool HasElements<T>(IEnumerable<T>? collection)
    {
        return collection?.Any() == true;
    }

    /// <summary>
    /// Verifica se uma collection tem um número específico de elementos
    /// </summary>
    public static bool HasExactCount<T>(IEnumerable<T>? collection, int expectedCount)
    {
        return collection?.Count() == expectedCount;
    }

    /// <summary>
    /// Verifica se uma collection tem pelo menos um número mínimo de elementos
    /// </summary>
    public static bool HasMinCount<T>(IEnumerable<T>? collection, int minCount)
    {
        return collection?.Count() >= minCount;
    }

    /// <summary>
    /// Verifica se uma collection tem no máximo um número específico de elementos
    /// </summary>
    public static bool HasMaxCount<T>(IEnumerable<T>? collection, int maxCount)
    {
        return collection?.Count() <= maxCount;
    }
}
