using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetUserById;

/// <summary>
/// Query to get user by ID.
/// </summary>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;
