namespace Outbox.Domain;

public abstract class AggregateRoot : Entity
{
    public List<IDomainEvent> _domaindEvents = new();

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() { return _domaindEvents.ToList(); }

    public void ClearDomainEvents() => _domaindEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domaindEvents.Add(domainEvent);
}