using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.Commands.Register;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Auth.Application.Interfaces.Security;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Application.Commands.Register;

/// <summary>
/// Handler for RegisterCommand.
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RegisterCommandHandler(
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

    public async Task<Result<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Validate username and email uniqueness
        var username = Username.From(request.Username);
        var email = Email.From(request.Email);

        if (await _userRepository.IsUsernameTakenAsync(username, cancellationToken))
            return Result.Failure<AuthResultDto>(Error.Conflict("Username is already taken"));

        if (await _userRepository.IsEmailRegisteredAsync(email, cancellationToken))
            return Result.Failure<AuthResultDto>(Error.Conflict("Email is already registered"));

        if (request.Phone != null && await _userRepository.IsPhoneRegisteredAsync(PhoneNumber.From(request.Phone), cancellationToken))
            return Result.Failure<AuthResultDto>(Error.Conflict("Phone number is already registered"));

        // Validate password complexity
        Password.ValidatePlainText(request.Password);

        // Hash password
        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var password = Password.FromHash(hash, salt);

        // Create user
        var user = User.Create(
            username: request.Username,
            email: request.Email,
            hashedPassword: password,
            phone: request.Phone,
            branchId: request.BranchId);

        await _userRepository.AddAsync(user, cancellationToken);

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
