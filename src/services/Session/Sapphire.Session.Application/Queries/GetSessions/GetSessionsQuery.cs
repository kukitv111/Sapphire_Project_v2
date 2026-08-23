using MediatR;
using Sapphire.Session.Application.DTOs;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Application.Queries.GetSessions;

/// <summary>
/// Запрос списка последних сессий для админ-панели.
/// </summary>
public sealed record GetSessionsQuery(int Limit = 50) : IRequest<Result<IReadOnlyList<SessionDto>>>;

public sealed class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, Result<IReadOnlyList<SessionDto>>>
{
    private const int MaxLimit = 200;

    private readonly ISessionRepository _sessionRepository;

    public GetSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var sessions = await _sessionRepository.GetRecentAsync(limit, cancellationToken);
        var dtos = sessions.Select(s => new SessionDto(
            s.Id, s.ComputerId, s.UserId, s.TimeSlot.Start, s.TimeSlot.End, s.Status.ToString())).ToList();
        return Result.Success<IReadOnlyList<SessionDto>>(dtos);
    }
}
