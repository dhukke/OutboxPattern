using ApiIntegrationLog.Api.Database;
using Outbox.Application;

namespace OutboxMongoDb;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly MongoDbContext _context;

    public UnitOfWork(MongoDbContext context)
        => _context = context;

    public void Dispose() => _context.Dispose();

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
