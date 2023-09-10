using ApiIntegrationLog.Api.Database;
using Outbox.Application;
using Outbox.Domain;

namespace OutboxMongoDb;

public sealed class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
        => _context = context;

    public void Insert(User user)
        => _context.AddCommand(
            () => _context.Users.InsertOneAsync(user),
            user
        );
}