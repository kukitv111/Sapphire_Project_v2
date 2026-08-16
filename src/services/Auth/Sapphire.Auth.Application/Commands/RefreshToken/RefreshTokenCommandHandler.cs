using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.Commands.RefreshToken;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Abstractions.Security;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IMapper mapper)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Hash the provided token and find it
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
            return Result.Failure<AuthResultDto>(Error.Unauthorized("Refresh token is invalid or expired"));

        // Get the user
        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user == null || !user.CanLogin())
            return Result.Failure<AuthResultDto>(Error.Unauthorized("User account is not active"));

        // Revoke old token (rotation)
        storedToken.MarkAsReplaced(Guid.NewGuid(), "Token rotated");

        // Generate new tokens
        var tokens = await _tokenService.GenerateTokensAsync(user, request.DeviceInfo, request.IpAddress, cancellationToken);

        // Map to DTO
        var userDto = _mapper.Map<UserDto>(user);

        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResultDto
        {
            User = userDto,
            Tokens = tokens
        });
    }
}
