using MediatR;

namespace OutboxEfCore;

public sealed record CreateUserCommand(string Name) : IRequest<User>;

public sealed class CreateUserCommandHander : IRequestHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHander(
        IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = User.Create(request.Name);

        _userRepository.Insert(user);

        await _unitOfWork.SaveChangesAsync();

        return user;
    }
}