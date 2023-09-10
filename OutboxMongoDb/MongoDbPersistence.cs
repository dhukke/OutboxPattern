namespace OutboxMongoDb;

public static class MongoDbPersistence
{
    public static void Configure()
    {
        AggregateRootConfiguration.Configure();
        UserConfiguration.Configure();
    }
}
