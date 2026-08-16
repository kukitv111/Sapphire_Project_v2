using System.Text.Json;

namespace Sapphire.Shared.Messaging.Outbox;

/// <summary>
/// Represents a message stored in the outbox for reliable event delivery.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates an OutboxMessage from a domain event.
    /// </summary>
    public static OutboxMessage Create<T>(T domainEvent) where T : class
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(T).AssemblyQualifiedName ?? typeof(T).Name,
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Deserializes the content to the specified type.
    /// </summary>
    public T? Deserialize<T>() where T : class
    {
        return JsonSerializer.Deserialize<T>(Content);
    }
}
