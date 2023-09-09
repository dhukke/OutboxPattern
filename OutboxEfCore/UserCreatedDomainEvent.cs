namespace OutboxEfCore;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;