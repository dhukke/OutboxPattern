using ApiIntegrationLog.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Newtonsoft.Json;
using Outbox.Domain;
using Outbox.Infrastructure.EfCore;
using Quartz;

namespace OutboxMongoDb;

[DisallowConcurrentExecution]
public class OutboxMessageProcessorJob : IJob
{
    private readonly MongoDbContext _dataContext;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxMessageProcessorJob> _logger;

    public OutboxMessageProcessorJob(
        MongoDbContext dataContext,
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

        await _dataContext.OutboxMessages
            .ReplaceOneAsync(
                Builders<OutboxMessage>.Filter.Eq("_id", outboxMessage.Id),
                outboxMessage,
                cancellationToken: cancellationToken
            );
    }

    private async Task<List<OutboxMessage>> GetAChunkOfOutboxMessages(ushort chunkSize)
        => await _dataContext
            .OutboxMessages
            .Find(message => message.ProcessedOnUtc == null)
            .Limit(chunkSize)
            .ToListAsync();
}
