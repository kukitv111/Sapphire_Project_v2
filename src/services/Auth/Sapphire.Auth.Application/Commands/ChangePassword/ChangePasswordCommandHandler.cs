using MediatR;
using Sapphire.Auth.Domain.Exceptions;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Abstractions.Security;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.ChangePassword;

/// <summary>
/// Handler for ChangePasswordCommand.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            return Result.Failure(Error.Create("USER_NOT_FOUND", "User not found"));
        }

        // 2. Verify current password
        var currentPasswordValid = _passwordHasher.VerifyPassword(
            request.CurrentPassword,
            user.Password.Hash,
            user.Password.Salt);

        if (!currentPasswordValid)
        {
            return Result.Failure(Error.Create("INVALID_PASSWORD", "Current password is incorrect"));
        }

        // 3. Validate and hash new password
        Password.ValidatePlainText(request.NewPassword);
        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        var hashedPassword = Password.FromHash(hash, salt);

        // 4. Change password (domain method)
        user.ChangePassword(hashedPassword, request.UserId);

        // 5. Revoke all refresh tokens (security measure)
        user.RevokeAllRefreshTokens("Password changed");

        // 6. Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
