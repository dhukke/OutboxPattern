using Outbox.Domain;

namespace Outbox.Application;

public interface IUserRepository
{
    void Insert(User user);
}