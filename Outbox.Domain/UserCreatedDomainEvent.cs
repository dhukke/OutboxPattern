namespace Outbox.Domain;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;