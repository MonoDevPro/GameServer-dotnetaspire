namespace GameServer.CharacterService.Domain.ValueObjects;

/// <summary>
/// Value object representando o identificador único de um item
/// </summary>
public record ItemId
{
    public string Value { get; }

    private ItemId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Item ID cannot be empty", nameof(value));
            
        Value = value;
    }

    public static ItemId From(string value)
    {
        return new ItemId(value);
    }

    public override string ToString()
    {
        return Value;
    }
}