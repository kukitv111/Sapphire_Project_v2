namespace Sapphire.Shared.Messaging.Outbox;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}

public sealed record EventEnvelope(Guid EventId, string Type, string Content, DateTime OccurredOn);
