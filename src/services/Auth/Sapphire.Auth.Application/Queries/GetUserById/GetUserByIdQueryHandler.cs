using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetUserById;

/// <summary>
/// Handler for GetUserByIdQuery.
/// </summary>
public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>(Error.NotFound("User not found"));

        var userDto = _mapper.Map<UserDto>(user);
        return Result.Success(userDto);
    }
}
