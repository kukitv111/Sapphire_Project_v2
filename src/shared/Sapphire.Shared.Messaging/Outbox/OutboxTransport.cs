using System.Net.Http.Json;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sapphire.Shared.Messaging.Outbox;

public interface IIncomingEventHandler
{
    Task HandleAsync(EventEnvelope message, CancellationToken ct);
}

public sealed class NoOpIncomingEventHandler : IIncomingEventHandler
{
    public Task HandleAsync(EventEnvelope message, CancellationToken ct) => Task.CompletedTask;
}

public static class OutboxTransport
{
    public static IServiceCollection AddOutboxTransport<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<InboxProcessor<TContext>>();
        services.AddHttpClient("outbox", client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddHostedService<OutboxDispatcher<TContext>>();
        return services;
    }

    public static void MapOutboxInbox<TContext>(this WebApplication app) where TContext : DbContext
    {
        app.MapMethods("/internal/events", ["POST"], async (HttpContext http, EventEnvelope message,
            InboxProcessor<TContext> inbox, IConfiguration config, CancellationToken ct) =>
        {
            var secret = config["Messaging:SharedSecret"];
            var supplied = http.Request.Headers["X-Sapphire-Message-Key"].ToString();
            if (string.IsNullOrWhiteSpace(secret) || supplied.Length != secret.Length ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(secret)))
                return Results.Unauthorized();
            if (message.EventId == Guid.Empty || message.Type.Length > 160 || message.Content.Length > 1_000_000)
                return Results.BadRequest();
            await inbox.ReceiveAsync(message, ct);
            return Results.Ok();
        }).AllowAnonymous();
    }
}

public sealed class InboxProcessor<TContext>(TContext db, IIncomingEventHandler handler)
    where TContext : DbContext
{
    public async Task ReceiveAsync(EventEnvelope message, CancellationToken ct = default)
    {
        if (db.Database.IsRelational()) await db.Database.BeginTransactionAsync(ct);
        try
        {
            if (!await db.Set<InboxMessage>().AnyAsync(m => m.EventId == message.EventId, ct))
            {
                await handler.HandleAsync(message, ct);
                db.Set<InboxMessage>().Add(new InboxMessage
                {
                    EventId = message.EventId, Type = message.Type, ReceivedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
            }
            if (db.Database.CurrentTransaction is { } transaction) await transaction.CommitAsync(ct);
        }
        catch
        {
            if (db.Database.CurrentTransaction is { } transaction) await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

public sealed class OutboxDispatcher<TContext>(IServiceScopeFactory scopes,
    IHttpClientFactory clients, IConfiguration config, ILogger<OutboxDispatcher<TContext>> logger)
    : BackgroundService where TContext : DbContext
{
    private long _deadLetterCount;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var meter = new Meter("Sapphire.Messaging");
        meter.CreateObservableGauge("sapphire_outbox_dead_letter",
            () => Interlocked.Read(ref _deadLetterCount));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Outbox dispatch iteration failed"); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    public async Task DispatchOnceAsync(CancellationToken ct = default)
    {
        var destinations = config.GetSection("Messaging:Subscribers").Get<string[]>() ?? [];
        if (destinations.Length == 0) return;
        var secret = config["Messaging:SharedSecret"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Messaging shared secret is required");
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var now = DateTime.UtcNow;
        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.DeadLetterAt == null &&
                (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.OccurredOn).Take(50).ToListAsync(ct);
        foreach (var message in pending)
        {
            try
            {
                if (!message.Type.EndsWith(".v1", StringComparison.Ordinal))
                    message.Type = EventNames.FromLegacy(message.Type);
                foreach (var destination in destinations)
                {
                    var client = clients.CreateClient("outbox");
                    using var request = new HttpRequestMessage(HttpMethod.Post, destination.TrimEnd('/') + "/internal/events")
                    {
                        Content = JsonContent.Create(new EventEnvelope(message.Id, message.Type, message.Content, message.OccurredOn))
                    };
                    request.Headers.Add("X-Sapphire-Message-Key", secret);
                    request.Headers.Add("X-Correlation-ID", message.Id.ToString("N"));
                    using var response = await client.SendAsync(request, ct);
                    response.EnsureSuccessStatusCode();
                }
                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
                message.NextAttemptAt = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RetryCount++;
                message.Error = ex.GetType().Name + ": " + ex.Message[..Math.Min(ex.Message.Length, 400)];
                if (message.RetryCount >= 5)
                {
                    message.DeadLetterAt = DateTime.UtcNow;
                    logger.LogError("Outbox message {MessageId} moved to dead-letter after {Attempts} attempts",
                        message.Id, message.RetryCount);
                }
                else
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(60, 2 << message.RetryCount));
            }
            await db.SaveChangesAsync(ct);
        }
        Interlocked.Exchange(ref _deadLetterCount,
            await db.Set<OutboxMessage>().LongCountAsync(m => m.DeadLetterAt != null, ct));
    }
}
