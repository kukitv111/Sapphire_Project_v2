using AutoMapper;
using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetUsers;

/// <summary>
/// Query to list users for the admin panel.
/// </summary>
public sealed record GetUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetActiveUsersAsync(cancellationToken);
        var dtos = users.Select(u => _mapper.Map<UserDto>(u)).ToList();
        return Result.Success<IReadOnlyList<UserDto>>(dtos);
    }
}
