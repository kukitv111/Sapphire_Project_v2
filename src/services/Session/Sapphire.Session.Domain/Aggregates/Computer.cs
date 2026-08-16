using Sapphire.Session.Domain.Events;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Session.Domain.Aggregates;

public sealed class Computer : AggregateRoot
{
    public string Model { get; private set; }
    public ComputerStatus Status { get; private set; }
    public DateTime? LastMaintenance { get; private set; }

    // EF Core constructor
    private Computer()
    {
        Model = string.Empty;
    }

    public Computer(string model)
    {
        Model = model;
        Status = ComputerStatus.Available;
        AddDomainEvent(new ComputerAddedEvent(Id, model));
    }

    public Result StartSession(Guid sessionId)
    {
        if (Status != ComputerStatus.Available)
            return Result.Failure(Error.Create("COMPUTER_BUSY", "Computer is not available"));

        Status = ComputerStatus.InUse;
        AddDomainEvent(new SessionStartedEvent(sessionId, Id, DateTime.UtcNow));
        return Result.Success();
    }

    public void EndSession()
    {
        Status = ComputerStatus.Available;
        AddDomainEvent(new SessionEndedEvent(Id, DateTime.UtcNow));
    }

    public void ScheduleMaintenance()
    {
        Status = ComputerStatus.Maintenance;
        LastMaintenance = DateTime.UtcNow;
        AddDomainEvent(new ComputerMaintenanceScheduledEvent(Id));
    }
}

public enum ComputerStatus
{
    Available,
    InUse,
    Maintenance
}
