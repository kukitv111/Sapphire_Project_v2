using MediatR;
using Sapphire.Session.Application;
using Sapphire.Session.Application.DTOs;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Session.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Common;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Session.Application.Commands.StartSession;

public record StartSessionCommand(Guid ComputerId, Guid UserId, DateTime StartTime, DateTime EndTime,
    Guid EntitlementId = default) : IRequest<Result<SessionDto>>;

public sealed class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, Result<SessionDto>>
{
    private readonly IComputerRepository _computerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBillingReservationClient _billing;

    public StartSessionCommandHandler(IComputerRepository computerRepository, ISessionRepository sessionRepository,
        IUnitOfWork unitOfWork, IBillingReservationClient billing)
    {
        _computerRepository = computerRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _billing = billing;
    }

    public async Task<Result<SessionDto>> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.EntitlementId == Guid.Empty)
            return Result.Failure<SessionDto>(Error.Validation("EntitlementId is required"));
        // 1. Validate computer availability
        var computerResult = await _computerRepository.GetByIdAsync(request.ComputerId, cancellationToken);
        if (computerResult.IsFailure)
            return Result.Failure<SessionDto>(computerResult.Error);

        var computer = computerResult.Value;
        if (computer is null)
            return Result.Failure<SessionDto>(Error.NotFound("Computer not found"));

        // 2. Create time slot
        var timeSlotResult = SessionTimeSlot.Create(request.StartTime, request.EndTime);
        if (timeSlotResult.IsFailure)
            return Result.Failure<SessionDto>(timeSlotResult.Error);

        var timeSlot = timeSlotResult.Value;
        if (timeSlot is null)
            return Result.Failure<SessionDto>(Error.Create("INVALID_TIME_SLOT", "Time slot is required"));

        // Client controls duration, never the start time used for charging.
        var serverStart = DateTime.UtcNow;
        var serverSlot = SessionTimeSlot.Create(serverStart,
            serverStart + (timeSlot.End - timeSlot.Start)).Value!;
        var session = new SessionAggregate(request.ComputerId, request.UserId, serverSlot, request.EntitlementId);
        var startResult = computer.StartSession(session.Id);
        if (startResult.IsFailure)
            return Result.Failure<SessionDto>(startResult.Error);

        var reserve = await _billing.ReserveAsync(session.Id, request.UserId, request.EntitlementId,
            serverStart, serverSlot.End, cancellationToken);
        if (reserve.IsFailure)
            return Result.Failure<SessionDto>(reserve.Error);
        try
        {
            await _sessionRepository.AddAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception startFailure)
        {
            try
            {
                var release = await _billing.ReleaseAsync(session.Id, CancellationToken.None);
                if (release.IsFailure)
                    throw new InvalidOperationException("Billing rejected reservation release");
            }
            catch (Exception releaseFailure)
            {
                throw new InvalidOperationException(
                    $"Session {session.Id} did not start; reservation requires reconciliation",
                    new AggregateException(startFailure, releaseFailure));
            }
            throw;
        }

        return Result.Success(new SessionDto(
            session.Id, session.ComputerId, session.UserId, session.TimeSlot.Start, session.TimeSlot.End, session.Status.ToString()));
    }
}
