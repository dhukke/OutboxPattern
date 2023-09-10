using MongoDB.Bson.Serialization;
using Outbox.Domain;

namespace OutboxMongoDb;

public class AggregateRootConfiguration
{
    public static void Configure()
    {
        BsonClassMap.RegisterClassMap<AggregateRoot>(map =>
        {
            map.UnmapField(x => x._domaindEvents);
            map.SetIgnoreExtraElements(true);
        });
    }
}
