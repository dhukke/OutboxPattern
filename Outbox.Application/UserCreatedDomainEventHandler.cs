using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Outbox.Domain;
using Outbox.IntegrationEvents;

namespace Outbox.Application;

public sealed class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;
    private readonly IBus _bus;

    public UserCreatedDomainEventHandler(
        ILogger<UserCreatedDomainEventHandler> logger,
        IBus bus
    )
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserCreatedDomainEvent for user id {UserId} was handled", notification.UserId);

        await _bus.Publish(new UserCreatedIntegrationEvent(notification.UserId));
    }
}