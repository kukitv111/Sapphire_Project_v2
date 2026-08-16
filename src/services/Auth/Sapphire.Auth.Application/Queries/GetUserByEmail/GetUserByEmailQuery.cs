using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Queries.GetUserByEmail;

/// <summary>
/// Query to get user by email.
/// </summary>
public sealed record GetUserByEmailQuery(string Email) : IRequest<Result<UserDto>>;
