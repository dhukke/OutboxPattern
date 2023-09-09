using MassTransit;

namespace OutboxEfCore;

public class UserCreatedIntegrationEventConsumer : IConsumer<UserCreatedIntegrationEvent>
{
    private readonly ILogger<UserCreatedIntegrationEventConsumer> _logger;

    public UserCreatedIntegrationEventConsumer(ILogger<UserCreatedIntegrationEventConsumer> logger)
        => _logger = logger;

    public Task Consume(ConsumeContext<UserCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "UserCreatedIntegrationEventConsumer, consumed event: {@UserCreatedIntegrationEvent}.",
            context.Message
        );

        return Task.CompletedTask;
    }
}
