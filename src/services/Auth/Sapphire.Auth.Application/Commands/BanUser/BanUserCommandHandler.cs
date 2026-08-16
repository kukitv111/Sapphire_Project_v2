using MediatR;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.BanUser;

/// <summary>
/// Handler for BanUserCommand.
/// </summary>
public sealed class BanUserCommandHandler : IRequestHandler<BanUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BanUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            return Result.Failure(Error.Create("USER_NOT_FOUND", "User not found"));
        }

        // 2. Ban user (domain method)
        user.Ban(request.Reason, request.BannedBy);

        // 3. Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
