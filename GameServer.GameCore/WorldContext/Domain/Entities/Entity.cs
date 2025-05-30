
using GameServer.WorldSimulationService.Domain.Events;

namespace GameServer.WorldSimulationService.Domain.Entities;

public abstract class Entity
{
    public long Id { get; private set; } // identidade :contentReference[oaicite:8]{index=8}

    private readonly List<DomainEvent> _domainEvents = []; // eventos de domínio :contentReference[oaicite:9]{index=9}
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected Entity() { }

    protected Entity(long id) => Id = id;

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
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;
        if (Id == 0 || other.Id == 0) return false;
        return Id.Equals(other.Id);
    }

    public static bool operator ==(Entity? a, Entity? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);

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