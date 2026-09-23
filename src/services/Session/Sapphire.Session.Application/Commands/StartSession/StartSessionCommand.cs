using MediatR;
using Sapphire.Session.Application.DTOs;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Session.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Common;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Session.Application.Commands.StartSession;

public record StartSessionCommand(Guid ComputerId, Guid UserId, DateTime StartTime, DateTime EndTime) : IRequest<Result<SessionDto>>;

public sealed class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, Result<SessionDto>>
{
    private readonly IComputerRepository _computerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartSessionCommandHandler(IComputerRepository computerRepository, ISessionRepository sessionRepository, IUnitOfWork unitOfWork)
    {
        _computerRepository = computerRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SessionDto>> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
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

        // 3. Start session
        var session = new SessionAggregate(request.ComputerId, request.UserId, timeSlot);
        var startResult = computer.StartSession(session.Id);
        if (startResult.IsFailure)
            return Result.Failure<SessionDto>(startResult.Error);

        // 4. Persist session and computer state
        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Map to DTO
        return Result.Success(new SessionDto(
            session.Id, session.ComputerId, session.UserId, session.TimeSlot.Start, session.TimeSlot.End, session.Status.ToString()));
    }
}
