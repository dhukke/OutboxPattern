namespace Outbox.Domain;

public class User : AggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public User(string name) : base(Guid.NewGuid()) => Name = name;

    public static User Create(string name)
    {
        var user = new User(name);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }
}