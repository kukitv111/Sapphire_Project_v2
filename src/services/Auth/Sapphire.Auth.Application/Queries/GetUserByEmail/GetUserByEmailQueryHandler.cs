using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.ValueObjects;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetUserByEmail;

/// <summary>
/// Handler for GetUserByEmailQuery.
/// </summary>
public sealed class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByEmailQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var email = Email.From(request.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>(Error.NotFound("User not found"));

        var userDto = _mapper.Map<UserDto>(user);
        return Result.Success(userDto);
    }
}
