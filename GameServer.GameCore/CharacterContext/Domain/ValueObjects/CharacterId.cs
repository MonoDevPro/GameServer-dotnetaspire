namespace GameServer.CharacterService.Domain.ValueObjects;

/// <summary>
/// Value object representando o identificador único de um personagem
/// </summary>
public record CharacterId
{
    public Guid Value { get; }

    private CharacterId(Guid value)
    {
        Value = value;
    }

    public static CharacterId Create()
    {
        return new CharacterId(Guid.NewGuid());
    }

    public static CharacterId From(Guid value)
    {
        return new CharacterId(value);
    }

    public static CharacterId From(string value)
    {
        return new CharacterId(Guid.Parse(value));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}