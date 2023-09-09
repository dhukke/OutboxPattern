using Outbox.Application;
using Outbox.Domain;

namespace Outbox.Infrastructure.EfCore;

public class UserRepository : IUserRepository
{
    public readonly DataContext _dataContext;

    public UserRepository(DataContext dataContext) => _dataContext = dataContext;

    public void Insert(User user) => _dataContext.Add(user);
}