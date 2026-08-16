using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.Commands.Logout;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result.Failure(Error.NotFound("User not found"));

        if (request.RevokeAll)
        {
            user.RevokeAllRefreshTokens("User logged out");
        }
        else if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (token != null && token.UserId == request.UserId)
            {
                user.RevokeRefreshToken(token.Id, "User logged out");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
