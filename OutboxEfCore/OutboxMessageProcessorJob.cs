using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;

namespace OutboxEfCore;

[DisallowConcurrentExecution]
public class OutboxMessageProcessorJob : IJob
{
    private readonly DataContext _dataContext;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxMessageProcessorJob> _logger;

    public OutboxMessageProcessorJob(
        DataContext dataContext,
        IPublisher publisher,
        ILogger<OutboxMessageProcessorJob> logger
    )
    {
        _dataContext = dataContext;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var outboxMessages = await GetAChunkOfOutboxMessages(20);

            foreach (var outboxMessage in outboxMessages)
            {
                await PublishAndMarkAsProcessedOutboxMessage(outboxMessage, context.CancellationToken);
            }

            await _dataContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Not possible to finish processing outbox message.");

            throw;
        }
    }

    private async Task PublishAndMarkAsProcessedOutboxMessage(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
            outboxMessage.Content,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }
        );

        if (domainEvent is null)
        {
            _logger.LogWarning("Unable to deserialize message {@message}", outboxMessage);

            return;
        }

        await _publisher.Publish(domainEvent, cancellationToken);

        outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
    }

    private async Task<List<OutboxMessage>> GetAChunkOfOutboxMessages(ushort chunkSize)
        => await _dataContext
            .Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .Take(chunkSize)
            .ToListAsync();
}
