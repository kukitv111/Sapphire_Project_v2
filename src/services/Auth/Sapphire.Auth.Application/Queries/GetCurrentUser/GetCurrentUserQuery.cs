using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetCurrentUser;

/// <summary>
/// Query to get current authenticated user.
/// </summary>
public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;
