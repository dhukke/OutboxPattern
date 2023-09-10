using MongoDB.Bson.Serialization;
using Outbox.Domain;

namespace OutboxMongoDb;

public class UserConfiguration
{
    public static void Configure()
        => BsonClassMap.RegisterClassMap<User>(
            map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
            }
        );
}
