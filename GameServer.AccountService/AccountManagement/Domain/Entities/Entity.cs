using GameServer.AccountService.AccountManagement.Domain.Events.Base;

namespace GameServer.AccountService.AccountManagement.Domain.Entities;

public abstract class Entity<TIdentity>
    where TIdentity : notnull
{
    public TIdentity Id { get; private set; } = default(TIdentity)!; // identidade
    public Guid UniqueId { get; private set; } = Guid.NewGuid(); // identidade única
    
    private readonly List<DomainEvent> _domainEvents = []; // eventos de domínio :contentReference[oaicite:9]{index=9}
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected Entity() { }

    protected Entity(TIdentity id, Guid uniqueId)
    {
        Id = id;
        UniqueId = uniqueId;
    }

    public void AddDomainEvent(DomainEvent eventItem)
        => _domainEvents.Add(eventItem);

    public void RemoveDomainEvent(DomainEvent eventItem)
        => _domainEvents.Remove(eventItem);

    public IReadOnlyCollection<DomainEvent> GetDomainEvents()
        => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
        => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TIdentity> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;
        //if (Id is null || other.Id is null) return false;
        return Id.Equals(other.Id);
    }

    public static bool operator ==(Entity<TIdentity>? a, Entity<TIdentity>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        //if (a.Id is null || b.Id is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity<TIdentity>? a, Entity<TIdentity>? b) => !(a == b);

    public override int GetHashCode()
        => (GetUnproxiedType(this).ToString() + Id)
            .GetHashCode(); // hash baseado em Id :contentReference[oaicite:10]{index=10}

    internal static Type GetUnproxiedType(object obj)
    {
        const string efCoreProxyPrefix = "Castle.Proxies.";
        const string nHibernateProxyPostfix = "Proxy";
        var type = obj.GetType();
        var name = type.ToString();
        if (name.Contains(efCoreProxyPrefix) || name.EndsWith(nHibernateProxyPostfix))
            return type.BaseType!;
        return type;
    }
}