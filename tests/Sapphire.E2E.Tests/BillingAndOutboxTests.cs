using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Api.Services;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Infrastructure.Persistence;
using Sapphire.Billing.Infrastructure.Persistence.Repositories;
using Sapphire.Billing.Infrastructure.Services;
using Sapphire.Session.Application;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Session.Domain.Events;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.E2E.Tests;

public sealed class BillingAndOutboxTests
{
    private sealed class TransportDb(DbContextOptions<TransportDb> options) : DbContext(options)
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<OutboxMessage>().HasKey(m => m.Id);
            model.Entity<InboxMessage>().HasKey(m => m.EventId);
        }
    }

    private sealed class CountingHandler : IIncomingEventHandler
    {
        public int Calls { get; private set; }
        public Task HandleAsync(EventEnvelope message, CancellationToken ct)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RelayHandler(Func<EventEnvelope, Task<HttpStatusCode>> receive) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var envelope = await request.Content!.ReadFromJsonAsync<EventEnvelope>(cancellationToken: ct);
            return new HttpResponseMessage(await receive(envelope!));
        }
    }

    private sealed class RelayFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ComputerPort(Computer computer) : IComputerRepository
    {
        public Task<Result<Computer>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Result.Success(computer));
        public Task AddAsync(Computer computer, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FailingSessionPort : ISessionRepository
    {
        public Task<Result<Sapphire.Session.Domain.Aggregates.Session>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<Sapphire.Session.Domain.Aggregates.Session>> GetRecentAsync(int limit,
            CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Sapphire.Session.Domain.Aggregates.Session session, CancellationToken ct = default)
            => throw new InvalidOperationException("Session database unavailable");
    }

    private sealed class SessionWork : Sapphire.Session.Domain.Repositories.IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class BillingSpy : IBillingReservationClient
    {
        public bool Reserved { get; private set; }
        public bool Released { get; private set; }
        public Task<Result> ReserveAsync(Guid sessionId, Guid userId, Guid entitlementId,
            DateTime startedAt, DateTime plannedEndAt, CancellationToken ct)
        {
            Reserved = true;
            return Task.FromResult(Result.Success());
        }
        public Task<Result> ReleaseAsync(Guid sessionId, CancellationToken ct)
        {
            Released = true;
            return Task.FromResult(Result.Success());
        }
    }

    private static BillingDbContext NewDb(string name) => new(new DbContextOptionsBuilder<BillingDbContext>()
        .UseInMemoryDatabase(name).Options);

    private static AssignTariffToUserCommandHandler PurchaseHandler(BillingDbContext db)
        => new(new TariffRepository(db), new WalletRepository(db), new EntitlementRepository(db),
            new Sapphire.Billing.Infrastructure.Persistence.UnitOfWork(db));

    private static async Task<(Guid User, Tariff Tariff, Wallet Wallet)> SeedAsync(BillingDbContext db, long balance = 1_000)
    {
        var user = Guid.NewGuid();
        var tariff = Tariff.Create("Minute", TariffType.PerMinute, pricePerMinuteCents: 10);
        var wallet = new Wallet(user, Money.FromCents(balance));
        db.Tariffs.Add(tariff);
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (user, tariff, wallet);
    }

    [Fact]
    public async Task Purchase_debits_once_and_replay_returns_same_entitlement()
    {
        var name = Guid.NewGuid().ToString();
        await using var db = NewDb(name);
        var (user, tariff, _) = await SeedAsync(db);
        var command = new AssignTariffToUserCommand
        { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-1" };
        var first = await PurchaseHandler(db).Handle(command, default);
        var replay = await PurchaseHandler(db).Handle(command, default);
        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.Id, replay.Value!.Id);
        Assert.Equal(600, first.Value.PurchasedCents);
        await using var verify = NewDb(name);
        Assert.Equal(400, (await verify.Wallets.SingleAsync()).MainBalance.Cents);
        Assert.Single(await verify.Entitlements.ToListAsync());
        Assert.Single(await verify.WalletTransactions.ToListAsync());
    }

    [Fact]
    public async Task Purchase_rejects_insufficient_funds_without_side_effects()
    {
        var name = Guid.NewGuid().ToString();
        await using var db = NewDb(name);
        var (user, tariff, _) = await SeedAsync(db, 100);
        var result = await PurchaseHandler(db).Handle(new AssignTariffToUserCommand
        { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-2" }, default);
        Assert.True(result.IsFailure);
        await using var verify = NewDb(name);
        Assert.Equal(100, (await verify.Wallets.SingleAsync()).MainBalance.Cents);
        Assert.Empty(await verify.Entitlements.ToListAsync());
        Assert.Empty(await verify.WalletTransactions.ToListAsync());
    }

    [Fact]
    public async Task Two_sessions_cannot_reserve_the_same_prepaid_balance()
    {
        var name = Guid.NewGuid().ToString();
        await using (var setup = NewDb(name))
        {
            var (user, tariff, _) = await SeedAsync(setup);
            var purchase = await PurchaseHandler(setup).Handle(new AssignTariffToUserCommand
            { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-3" }, default);
            Assert.True(purchase.IsSuccess);
        }
        var entitlement = await NewDb(name).Entitlements.SingleAsync();
        var start = DateTime.UtcNow;
        async Task<bool> Reserve()
        {
            await using var db = NewDb(name);
            var request = new ReserveRequest(Guid.NewGuid(), entitlement.UserId, entitlement.Id,
                start, start.AddMinutes(50));
            var result = await new BillingReservationService(db).ReserveAsync(request);
            return result.IsSuccess;
        }
        var outcomes = await Task.WhenAll(Reserve(), Reserve());
        Assert.Single(outcomes, success => success);
        await using var verify = NewDb(name);
        Assert.Equal(100, (await verify.Entitlements.SingleAsync()).AvailableCents);
        Assert.Single(await verify.SessionReservations.ToListAsync());
    }

    [Fact]
    public async Task Early_completion_refunds_unused_reserve_and_duplicate_event_is_noop()
    {
        var name = Guid.NewGuid().ToString();
        await using var db = NewDb(name);
        var (user, tariff, _) = await SeedAsync(db);
        var purchase = await PurchaseHandler(db).Handle(new AssignTariffToUserCommand
        { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-4" }, default);
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var billing = new BillingReservationService(db);
        Assert.True((await billing.ReserveAsync(new ReserveRequest(sessionId, user, purchase.Value!.Id,
            start, start.AddMinutes(30)))).IsSuccess);
        var payload = JsonSerializer.Serialize(new { SessionId = sessionId, CompletedAt = start.AddMinutes(5) });
        var envelope = new EventEnvelope(Guid.NewGuid(), "session.completed.v1", payload, DateTime.UtcNow);
        var inbox = new InboxProcessor<BillingDbContext>(db, new SessionBillingEventHandler(billing));
        await inbox.ReceiveAsync(envelope);
        await inbox.ReceiveAsync(envelope);
        await inbox.ReceiveAsync(envelope with { EventId = Guid.NewGuid() });
        Assert.Equal(550, (await db.Entitlements.SingleAsync()).AvailableCents);
        Assert.Equal(50, (await db.SessionReservations.SingleAsync()).ChargedCents);
        Assert.Equal(2, await db.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task Cancellation_refunds_entire_reservation()
    {
        var name = Guid.NewGuid().ToString();
        await using var db = NewDb(name);
        var (user, tariff, _) = await SeedAsync(db);
        var purchase = await PurchaseHandler(db).Handle(new AssignTariffToUserCommand
        { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-5" }, default);
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var billing = new BillingReservationService(db);
        Assert.True((await billing.ReserveAsync(new ReserveRequest(sessionId, user, purchase.Value!.Id,
            start, start.AddMinutes(30)))).IsSuccess);
        await new InboxProcessor<BillingDbContext>(db, new SessionBillingEventHandler(billing))
            .ReceiveAsync(new EventEnvelope(Guid.NewGuid(), "session.cancelled.v1",
                JsonSerializer.Serialize(new { SessionId = sessionId, CancelledAt = start }), DateTime.UtcNow));
        Assert.Equal(600, (await db.Entitlements.SingleAsync()).AvailableCents);
    }

    private sealed class FailOnce : SaveChangesInterceptor
    {
        public bool Fail { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail) { Fail = false; throw new InvalidOperationException("write failed"); }
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task Purchase_write_failure_does_not_debit_or_create_entitlement()
    {
        var name = Guid.NewGuid().ToString();
        var interceptor = new FailOnce();
        await using var db = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(name).AddInterceptors(interceptor).Options);
        var (user, tariff, _) = await SeedAsync(db);
        interceptor.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => PurchaseHandler(db).Handle(
            new AssignTariffToUserCommand
            { UserId = user, TariffId = tariff.Id, IdempotencyKey = "checkout-fail" }, default));
        await using var verify = NewDb(name);
        Assert.Equal(1_000, (await verify.Wallets.SingleAsync()).MainBalance.Cents);
        Assert.Empty(await verify.Entitlements.ToListAsync());
    }

    [Fact]
    public async Task Failed_session_start_compensates_the_remote_reservation()
    {
        var computer = new Computer("PC-1");
        var billing = new BillingSpy();
        var handler = new StartSessionCommandHandler(new ComputerPort(computer),
            new FailingSessionPort(), new SessionWork(), billing);
        var now = DateTime.UtcNow;
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new StartSessionCommand(computer.Id, Guid.NewGuid(), now, now.AddMinutes(20), Guid.NewGuid()),
            default));
        Assert.True(billing.Reserved);
        Assert.True(billing.Released);
    }

    [Fact]
    public async Task Dispatcher_retries_after_consumer_recovery_and_inbox_deduplicates()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TransportDb>(o => o.UseInMemoryDatabase(databaseName));
        using var provider = services.BuildServiceProvider();
        var handler = new CountingHandler();
        var available = false;
        var relay = new RelayHandler(async message =>
        {
            if (!available) return HttpStatusCode.ServiceUnavailable;
            using var scope = provider.CreateScope();
            await new InboxProcessor<TransportDb>(scope.ServiceProvider.GetRequiredService<TransportDb>(), handler)
                .ReceiveAsync(message);
            return HttpStatusCode.OK;
        });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Subscribers:0"] = "http://billing.local",
            ["Messaging:SharedSecret"] = "local-test-secret"
        }).Build();
        var dispatcher = new OutboxDispatcher<TransportDb>(provider.GetRequiredService<IServiceScopeFactory>(),
            new RelayFactory(relay), config, NullLogger<OutboxDispatcher<TransportDb>>.Instance);
        Guid id;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            var message = OutboxMessage.Create(new Sapphire.Session.Domain.Events.SessionCompletedEvent(
                Guid.NewGuid(), DateTime.UtcNow));
            id = message.Id;
            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
        }
        await dispatcher.DispatchOnceAsync();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            var message = await db.OutboxMessages.SingleAsync();
            Assert.Equal(1, message.RetryCount);
            Assert.Null(message.ProcessedOn);
            message.NextAttemptAt = null;
            await db.SaveChangesAsync();
        }
        available = true;
        await dispatcher.DispatchOnceAsync();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            Assert.NotNull((await db.OutboxMessages.SingleAsync()).ProcessedOn);
            Assert.Equal(id, (await db.InboxMessages.SingleAsync()).EventId);
        }
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void Legacy_assembly_type_converts_to_versioned_event_name()
    {
        var old = typeof(SessionCompletedEvent).AssemblyQualifiedName!;
        Assert.Equal("session.completed.v1", EventNames.FromLegacy(old));
    }

    [Fact]
    public async Task Dispatcher_dead_letters_after_five_permanent_failures()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TransportDb>(o => o.UseInMemoryDatabase(databaseName));
        using var provider = services.BuildServiceProvider();
        var relay = new RelayHandler(_ => Task.FromResult(HttpStatusCode.ServiceUnavailable));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Subscribers:0"] = "http://billing.local",
            ["Messaging:SharedSecret"] = "local-test-secret"
        }).Build();
        var dispatcher = new OutboxDispatcher<TransportDb>(provider.GetRequiredService<IServiceScopeFactory>(),
            new RelayFactory(relay), config, NullLogger<OutboxDispatcher<TransportDb>>.Instance);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            db.OutboxMessages.Add(OutboxMessage.Create(new Sapphire.Session.Domain.Events.SessionCompletedEvent(
                Guid.NewGuid(), DateTime.UtcNow)));
            await db.SaveChangesAsync();
        }
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await dispatcher.DispatchOnceAsync();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            (await db.OutboxMessages.SingleAsync()).NextAttemptAt = null;
            await db.SaveChangesAsync();
        }
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransportDb>();
            var message = await db.OutboxMessages.SingleAsync();
            Assert.Equal(5, message.RetryCount);
            Assert.NotNull(message.DeadLetterAt);
            Assert.Null(message.ProcessedOn);
        }
        await dispatcher.DispatchOnceAsync();
        using (var scope = provider.CreateScope())
            Assert.Equal(5, (await scope.ServiceProvider.GetRequiredService<TransportDb>()
                .OutboxMessages.SingleAsync()).RetryCount);
    }
}
