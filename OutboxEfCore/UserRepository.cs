namespace OutboxEfCore;

public class UserRepository : IUserRepository
{
    public readonly DataContext _dataContext;

    public UserRepository(DataContext dataContext) => _dataContext = dataContext;

    public void Insert(User user) => _dataContext.Add(user);
}