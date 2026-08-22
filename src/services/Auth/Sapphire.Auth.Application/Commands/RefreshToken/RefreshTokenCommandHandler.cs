using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Enums;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private const string InvalidRefreshTokenMessage = "Refresh token is invalid or expired";
    private const string TokenRotatedReason = "Token rotated";
    private const string ReplayDetectedReason = "Refresh token replay detected";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityHistoryRepository _activityHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IActivityHistoryRepository activityHistoryRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IMapper mapper)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _activityHistoryRepository = activityHistoryRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var storedToken = await _refreshTokenRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken);
            if (storedToken is null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AuthResultDto>(Error.Unauthorized(InvalidRefreshTokenMessage));
            }

            if (storedToken.IsRevoked)
            {
                await RevokeFamilyForReplayAsync(storedToken, request, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result.Failure<AuthResultDto>(Error.Unauthorized(InvalidRefreshTokenMessage));
            }

            if (storedToken.IsExpired)
            {
                storedToken.Revoke("Refresh token expired");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result.Failure<AuthResultDto>(Error.Unauthorized(InvalidRefreshTokenMessage));
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user is null || !user.CanLogin())
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AuthResultDto>(Error.Unauthorized("User account is not active"));
            }

            var (newRefreshToken, newRefreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
            var replacementToken = user.CreateRefreshToken(
                newRefreshTokenHash,
                newRefreshTokenExpiresAt,
                request.DeviceInfo,
                request.IpAddress,
                storedToken.FamilyId);

            storedToken.MarkAsReplaced(replacementToken.Id, TokenRotatedReason);

            await _activityHistoryRepository.AddAsync(
                ActivityHistory.Create(
                    user.Id,
                    ActivityType.TokenRefresh,
                    "Refresh token rotated",
                    entityType: "RefreshToken",
                    entityId: storedToken.Id,
                    metadata: $"{{\"familyId\":\"{storedToken.FamilyId}\",\"replacementTokenId\":\"{replacementToken.Id}\"}}",
                    ipAddress: request.IpAddress,
                    deviceInfo: request.DeviceInfo),
                cancellationToken);

            var userDto = _mapper.Map<UserDto>(user);
            var accessToken = _tokenService.GenerateAccessToken(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success(new AuthResultDto
            {
                User = userDto,
                Tokens = new TokenDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    RefreshTokenId = replacementToken.Id,
                    ExpiresAt = newRefreshTokenExpiresAt
                }
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task RevokeFamilyForReplayAsync(
        Sapphire.Auth.Domain.Entities.RefreshToken replayedToken,
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeFamilyAsync(replayedToken.FamilyId, ReplayDetectedReason, cancellationToken);
        await _activityHistoryRepository.AddAsync(
            ActivityHistory.Create(
                replayedToken.UserId,
                ActivityType.TokenRefresh,
                "Refresh token replay detected; token family revoked",
                entityType: "RefreshToken",
                entityId: replayedToken.Id,
                metadata: $"{{\"familyId\":\"{replayedToken.FamilyId}\",\"replayedTokenId\":\"{replayedToken.Id}\"}}",
                ipAddress: request.IpAddress,
                deviceInfo: request.DeviceInfo),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
