using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Abstractions.Security;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Application.Commands.Login;

/// <summary>
/// Handler for LoginCommand.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Load user by username or email
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.Login, cancellationToken);
        if (user == null)
            return Result.Failure<AuthResultDto>(Error.Unauthorized("Invalid credentials"));

        // Check if user can login
        if (!user.CanLogin())
        {
            if (user.IsLockedOut())
                return Result.Failure<AuthResultDto>(Error.Unauthorized("Account is locked. Try again later."));

            return Result.Failure<AuthResultDto>(Error.Unauthorized("Account is not active"));
        }

        // Verify password
        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.Password.Hash, user.Password.Salt);
        user.RecordAuthenticationAttempt(passwordValid);

        if (!passwordValid)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthResultDto>(Error.Unauthorized("Invalid credentials"));
        }

        // Record successful login
        user.RecordLogin(request.IpAddress ?? "unknown", request.DeviceInfo);

        // Generate tokens
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
