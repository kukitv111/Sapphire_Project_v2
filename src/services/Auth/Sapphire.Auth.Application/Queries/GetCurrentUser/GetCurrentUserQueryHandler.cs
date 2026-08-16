using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetCurrentUser;

/// <summary>
/// Handler for GetCurrentUserQuery.
/// </summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
            return Result.Failure<UserDto>(Error.Unauthorized("User is not authenticated"));

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>(Error.NotFound("User not found"));

        var userDto = _mapper.Map<UserDto>(user);
        return Result.Success(userDto);
    }
}
