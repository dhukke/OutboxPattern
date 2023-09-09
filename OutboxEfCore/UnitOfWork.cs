using Newtonsoft.Json;

namespace OutboxEfCore;

public class UnitOfWork : IUnitOfWork
{
    public readonly DataContext _dataContext;

    public UnitOfWork(DataContext dataContext) => _dataContext = dataContext;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutBoxMessages();

        return _dataContext.SaveChangesAsync(cancellationToken);
    }

    private void ConvertDomainEventsToOutBoxMessages()
    {
        var outboxMessages = _dataContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .SelectMany(aggregateRoot =>
            {
                var domainEvents = aggregateRoot.GetDomainEvents();

                aggregateRoot.ClearDomainEvents();

                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccuredOnUtc = DateTime.UtcNow, // change to datetime provider
                Type = domainEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(
                    domainEvent,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }
                )
            })
            .ToList();

        _dataContext.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}