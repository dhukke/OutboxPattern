using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Newtonsoft.Json;
using Outbox.Domain;
using Outbox.Infrastructure.EfCore;

namespace ApiIntegrationLog.Api.Database;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;
    private readonly ILogger<MongoDbContext>? _logger;
    private IClientSessionHandle? _session;

    private readonly List<(Func<Task>, AggregateRoot)> _commands;

    public IMongoDatabase GetDatabase() => _database;

    public IMongoCollection<User> Users { get; }
    public IMongoCollection<OutboxMessage> OutboxMessages { get; }

    public MongoDbContext(
        string connectionString,
        string databaseName,
        ILogger<MongoDbContext>? logger = null
    )
    {
        var mongoConnectionUrl = new MongoUrl(connectionString);
        var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);

        if (logger is not null)
        {
            _logger = logger;

            mongoClientSettings.ClusterConfigurator =
                cb =>
                cb.Subscribe<CommandStartedEvent>(e =>
                    _logger.LogInformation("{@CommandName} - {@Command}", e.CommandName, e.Command.ToJson())
                );
        }

        _client = new MongoClient(mongoClientSettings);
        _database = _client.GetDatabase(databaseName);

        Users = _database.GetCollection<User>("User");
        OutboxMessages = _database.GetCollection<OutboxMessage>("OutboxMessage");

        _commands = new List<(Func<Task>, AggregateRoot)>();
    }

    public void AddCommand(Func<Task> func, AggregateRoot aggregateRoot)
        => _commands.Add((func, aggregateRoot));

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using (_session = await _client.StartSessionAsync(cancellationToken: cancellationToken))
        {
            _session.StartTransaction();

            var commandTasks = _commands.Select(c => c.Item1());

            await Task.WhenAll(commandTasks);

            var outboxMessages = _commands.Select(x => x.Item2)
                .SelectMany(aggregateRoot =>
                {
                    var domainEvents = aggregateRoot.GetDomainEvents();

                    aggregateRoot.ClearDomainEvents();

                    return domainEvents;
                }
                )
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccuredOnUtc = DateTime.UtcNow,
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

            await OutboxMessages.InsertManyAsync(outboxMessages, cancellationToken: cancellationToken);

            await _session.CommitTransactionAsync(cancellationToken);
        }
    }

    public void Dispose() => _session?.Dispose();
}