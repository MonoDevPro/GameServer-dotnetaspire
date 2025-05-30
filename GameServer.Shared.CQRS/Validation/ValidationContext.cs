namespace GameServer.Shared.CQRS.Validation;

/// <summary>
/// Contexto de validação que contém informações sobre o objeto sendo validado
/// </summary>
/// <typeparam name="T">Tipo do objeto sendo validado</typeparam>
public class ValidationContext<T>
{
    public T Instance { get; }
    public string PropertyName { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; } = new();

    public ValidationContext(T instance)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
    }

    /// <summary>
    /// Adiciona uma propriedade customizada ao contexto
    /// </summary>
    public ValidationContext<T> WithProperty(string key, object value)
    {
        Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Define o nome da propriedade sendo validada
    /// </summary>
    public ValidationContext<T> ForProperty(string propertyName)
    {
        PropertyName = propertyName;
        return this;
    }

    /// <summary>
    /// Obtém uma propriedade customizada do contexto
    /// </summary>
    public TProperty? GetProperty<TProperty>(string key)
    {
        return Properties.TryGetValue(key, out var value) && value is TProperty typedValue
            ? typedValue
            : default;
    }
}
