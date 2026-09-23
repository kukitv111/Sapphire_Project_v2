using System.Text.Json;
using Sapphire.Billing.Infrastructure.Services;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Billing.Api.Services;

public sealed class SessionBillingEventHandler(BillingReservationService billing) : IIncomingEventHandler
{
    public async Task HandleAsync(EventEnvelope message, CancellationToken ct)
    {
        if (message.Type == "session.completed.v1")
        {
            var payload = JsonSerializer.Deserialize<SessionCompletedPayload>(message.Content)
                ?? throw new JsonException("Session completion payload is empty");
            await billing.SettleInCurrentTransactionAsync(payload.SessionId, payload.CompletedAt, ct);
        }
        else if (message.Type == "session.cancelled.v1")
        {
            var payload = JsonSerializer.Deserialize<SessionCancelledPayload>(message.Content)
                ?? throw new JsonException("Session cancellation payload is empty");
            await billing.ReleaseInCurrentTransactionAsync(payload.SessionId, ct);
        }
    }

    private sealed record SessionCompletedPayload(Guid SessionId, DateTime CompletedAt);
    private sealed record SessionCancelledPayload(Guid SessionId, DateTime CancelledAt);
}
