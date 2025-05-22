namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

public abstract class ValueObject<T> : IEquatable<T>
{
    // Construtor sem parâmetros para o EF Core
    protected ValueObject() { }
    
    public override bool Equals(object? obj)
    {
        if (obj is not T other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != obj.GetType()) return false;

        return EqualsCore(other);
    }

    public bool Equals(T? other) =>
        EqualsCore(other);

    public override int GetHashCode() =>
        ComputeHashCode();

    public static bool operator ==(ValueObject<T> a, ValueObject<T> b) =>
        ReferenceEquals(a, b) || (a?.Equals(b) ?? false);

    public static bool operator !=(ValueObject<T> a, ValueObject<T> b) =>
        !(a == b);

    /// <summary>
    /// Implementar comparação de valores aqui.
    /// </summary>
    protected abstract bool EqualsCore(T? other);

    /// <summary>
    /// Calcular hash-code com base nos mesmos membros de EqualsCore.
    /// </summary>
    protected abstract int ComputeHashCode();
}