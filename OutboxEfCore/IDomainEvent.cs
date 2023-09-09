using MediatR;

namespace OutboxEfCore;

public interface IDomainEvent : INotification
{
}