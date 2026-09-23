using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Application.Commands.RefreshToken;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Infrastructure.Persistence.Repositories;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Billing.Application.Commands.ApplyPromocode;
using Sapphire.Billing.Api.Services;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Infrastructure.Persistence;
using Sapphire.Billing.Infrastructure.Persistence.Repositories;
using Sapphire.Billing.Infrastructure.Services;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Session.Domain.Events;
using Sapphire.Session.Domain.ValueObjects;
using Sapphire.Session.Application;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Session.Infrastructure.Persistence;
using Sapphire.Session.Infrastructure.Persistence.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;
using Sapphire.Shared.Messaging.Outbox;

static string Required(string key) => Environment.GetEnvironmentVariable(key)
    ?? throw new InvalidOperationException($"{key} must point to a disposable PostgreSQL database");
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var authConnection = Required("SAPPHIRE_TEST_AUTH_CONNECTION");
var billingConnection = Required("SAPPHIRE_TEST_BILLING_CONNECTION");
var sessionConnection = Required("SAPPHIRE_TEST_SESSION_CONNECTION");
var billingOptions = new DbContextOptionsBuilder<BillingDbContext>().UseNpgsql(billingConnection).Options;
if (args.Contains("--crash-write"))
{
    await using var crashDb = new BillingDbContext(billingOptions);
    var crashWallet = new Wallet(Guid.NewGuid(), Money.FromCents(1_000));
    crashDb.Wallets.Add(crashWallet);
    await crashDb.SaveChangesAsync();
    crashDb.ChangeTracker.Clear();
    await using var transaction = await crashDb.Database.BeginTransactionAsync();
    var loaded = await crashDb.Wallets.SingleAsync(w => w.Id == crashWallet.Id);
    loaded.Deposit(Money.FromCents(500), "interrupted transaction");
    await new Sapphire.Billing.Infrastructure.Persistence.UnitOfWork(crashDb).SaveChangesAsync();
    Console.WriteLine("TX_WRITTEN:" + crashWallet.Id);
    Console.Out.Flush();
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}
if (args.Contains("--verify-crash"))
{
    var walletId = Guid.Parse(args[Array.IndexOf(args, "--verify-crash") + 1]);
    await using var verifyDb = new BillingDbContext(billingOptions);
    Check((await verifyDb.Wallets.SingleAsync(w => w.Id == walletId)).MainBalance.Cents == 1_000,
        "Uncommitted wallet write survived process termination");
    Check(!await verifyDb.OutboxMessages.AnyAsync(m => m.Content.Contains(walletId.ToString()) && m.Type == "wallet.deposited.v1"),
        "Uncommitted outbox event survived process termination");
    Console.WriteLine("Killed writer: wallet and outbox stayed consistent after restart.");
    return;
}

await using (var auth = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
    .UseNpgsql(authConnection).Options))
{
    Check(!(await auth.Database.GetPendingMigrationsAsync()).Any(), "Auth migrations pending");
}

await using var billing = new BillingDbContext(billingOptions);
Check(!(await billing.Database.GetPendingMigrationsAsync()).Any(), "Billing migrations pending");
var userId = Guid.NewGuid();
var tariff = Tariff.Create("Postgres smoke", TariffType.PerMinute, pricePerMinuteCents: 10);
var wallet = new Wallet(userId, Money.FromCents(1_000));
billing.Tariffs.Add(tariff);
billing.Wallets.Add(wallet);
await billing.SaveChangesAsync();
billing.ChangeTracker.Clear();
var purchaseHandler = new AssignTariffToUserCommandHandler(new TariffRepository(billing),
    new WalletRepository(billing), new EntitlementRepository(billing),
    new Sapphire.Billing.Infrastructure.Persistence.UnitOfWork(billing));
var purchaseCommand = new AssignTariffToUserCommand
{ UserId = userId, TariffId = tariff.Id, IdempotencyKey = Guid.NewGuid().ToString() };
var purchase = await purchaseHandler.Handle(purchaseCommand, default);
Check(purchase.IsSuccess, "Purchase failed");
var replay = await purchaseHandler.Handle(purchaseCommand, default);
Check(replay.Value?.Id == purchase.Value?.Id, "Purchase idempotency failed");
Check((await billing.Wallets.SingleAsync()).MainBalance.Cents == 400, "Wallet debited more than once");

var start = DateTime.UtcNow;
async Task<bool> Reserve(Guid id)
{
    await using var concurrentDb = new BillingDbContext(billingOptions);
    var result = await new BillingReservationService(concurrentDb).ReserveAsync(
        new ReserveRequest(id, userId, purchase.Value!.Id, start, start.AddMinutes(50)));
    return result.IsSuccess;
}
var firstSessionId = Guid.NewGuid();
var secondSessionId = Guid.NewGuid();
var outcomes = await Task.WhenAll(Reserve(firstSessionId), Reserve(secondSessionId));
Check(outcomes.Count(success => success) == 1, "Concurrent sessions spent the same prepaid credit");
var winningId = outcomes[0] ? firstSessionId : secondSessionId;
billing.ChangeTracker.Clear();
var completed = new SessionCompletedEvent(winningId, start.AddMinutes(5).AddSeconds(-1));
var outbound = OutboxMessage.Create(completed);
var envelope = new EventEnvelope(outbound.Id, outbound.Type, outbound.Content, outbound.OccurredOn);
var processor = new InboxProcessor<BillingDbContext>(billing,
    new SessionBillingEventHandler(new BillingReservationService(billing)));
await processor.ReceiveAsync(envelope);
await processor.ReceiveAsync(envelope);
billing.ChangeTracker.Clear();
Check((await billing.Entitlements.SingleAsync()).AvailableCents == 550,
    "Early completion did not return unused reserve exactly once");
Check((await billing.SessionReservations.SingleAsync()).ChargedCents == 50,
    "Actual session charge is wrong");

await using (var sessionDb = new SessionDbContext(new DbContextOptionsBuilder<SessionDbContext>()
    .UseNpgsql(sessionConnection).Options))
{
    Check(!(await sessionDb.Database.GetPendingMigrationsAsync()).Any(), "Session migrations pending");
    var computer = new Computer("Postgres smoke PC");
    var slot = SessionTimeSlot.Create(start, start.AddMinutes(10)).Value!;
    var session = new Sapphire.Session.Domain.Aggregates.Session(computer.Id, userId, slot, purchase.Value!.Id);
    computer.StartSession(session.Id);
    sessionDb.Computers.Add(computer);
    sessionDb.Sessions.Add(session);
    await new Sapphire.Session.Infrastructure.Persistence.UnitOfWork(sessionDb).SaveChangesAsync();
    session.Complete();
    computer.EndSession();
    await new Sapphire.Session.Infrastructure.Persistence.UnitOfWork(sessionDb).SaveChangesAsync();
    Check(await sessionDb.OutboxMessages.AnyAsync(m => m.Type == "session.completed.v1"),
        "Session completion was not queued for delivery");
}

Console.WriteLine("PostgreSQL migrations, purchase, concurrent reserve, settlement, inbox and outbox passed.");
