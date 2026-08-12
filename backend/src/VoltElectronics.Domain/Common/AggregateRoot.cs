namespace VoltElectronics.Domain.Common;

/// <summary>
/// Consistency boundary: the only kind of object a repository hands out or persists.
/// Invariants that span several entities are enforced by methods on the root, never by callers.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
