using MediatR;
using Sapphire.Session.Application.DTOs;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Application.Commands.FinishSession;

public sealed record FinishSessionCommand(Guid SessionId, Guid ActorId, bool IsStaff, bool Cancel)
    : IRequest<Result<SessionDto>>;

public sealed class FinishSessionCommandHandler(ISessionRepository sessions,
    IComputerRepository computers, IUnitOfWork unitOfWork)
    : IRequestHandler<FinishSessionCommand, Result<SessionDto>>
{
    public async Task<Result<SessionDto>> Handle(FinishSessionCommand request, CancellationToken ct)
    {
        var found = await sessions.GetByIdAsync(request.SessionId, ct);
        if (found.IsFailure || found.Value is null)
            return Result.Failure<SessionDto>(Error.NotFound("Session not found"));
        var session = found.Value;
        if (!request.IsStaff && session.UserId != request.ActorId)
            return Result.Failure<SessionDto>(Error.Forbidden("Cannot finish another user's session"));
        if (session.Status != Sapphire.Session.Domain.Aggregates.SessionStatus.Active)
            return Result.Failure<SessionDto>(Error.Conflict("Session is already closed"));
        var foundComputer = await computers.GetByIdAsync(session.ComputerId, ct);
        if (foundComputer.IsFailure || foundComputer.Value is null)
            return Result.Failure<SessionDto>(Error.NotFound("Computer not found"));
        if (request.Cancel) session.Cancel();
        else session.Complete();
        foundComputer.Value.EndSession();
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new SessionDto(session.Id, session.ComputerId, session.UserId,
            session.TimeSlot.Start, session.CompletedAt, session.Status.ToString()));
    }
}
