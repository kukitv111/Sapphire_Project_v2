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

// Two requests may reserve credit, but only one may leave an active session on a PC.
var startComputer = new Computer("Concurrent-start PC");
await using (var seedSession = new SessionDbContext(new DbContextOptionsBuilder<SessionDbContext>()
    .UseNpgsql(sessionConnection).Options))
{
    seedSession.Computers.Add(startComputer);
    await seedSession.SaveChangesAsync();
}
var startGate = new StartGateBillingClient(billingOptions);
async Task<bool> StartOnComputer()
{
    await using var db = new SessionDbContext(new DbContextOptionsBuilder<SessionDbContext>()
        .UseNpgsql(sessionConnection).Options);
    var handler = new StartSessionCommandHandler(new ComputerRepository(db), new SessionRepository(db),
        new Sapphire.Session.Infrastructure.Persistence.UnitOfWork(db), startGate);
    var now = DateTime.UtcNow;
    try
    {
        var result = await handler.Handle(new StartSessionCommand(startComputer.Id, userId,
            now, now.AddMinutes(20), purchase.Value!.Id), default);
        return result.IsSuccess;
    }
    catch (DbUpdateException) { return false; }
}
var sessionOutcomes = await Task.WhenAll(StartOnComputer(), StartOnComputer());
Check(sessionOutcomes.Count(x => x) == 1, "Concurrent PC start created two active sessions");
await using (var verifySession = new SessionDbContext(new DbContextOptionsBuilder<SessionDbContext>()
    .UseNpgsql(sessionConnection).Options))
{
    Check(await verifySession.Sessions.CountAsync(s => s.ComputerId == startComputer.Id) == 1,
        "Concurrent PC start persisted multiple sessions");
}
await using (var verifyBilling = new BillingDbContext(billingOptions))
{
    var reservations = await verifyBilling.SessionReservations.CountAsync(r => r.UserId == userId);
    Check(reservations is 2 or 3, "Unexpected concurrent PC reservation count");
    Check(await verifyBilling.SessionReservations.CountAsync(r => r.UserId == userId &&
        r.Status == Sapphire.Billing.Domain.Entities.ReservationStatus.Reserved) == 1,
        "Concurrent PC start left an orphaned reservation");
    Check(await verifyBilling.SessionReservations.CountAsync(r => r.UserId == userId &&
        r.Status == Sapphire.Billing.Domain.Entities.ReservationStatus.Released) == reservations - 2,
        "Failed PC start did not release its credit");
}
Console.WriteLine("Concurrent start on one PC: one session committed, losing reserve released.");

// Force both writers to operate from the same observed wallet balance.
await using (var debitDb = new BillingDbContext(billingOptions))
await using (var creditDb = new BillingDbContext(billingOptions))
{
    var debitWallet = await debitDb.Wallets.SingleAsync(w => w.Id == wallet.Id);
    var creditWallet = await creditDb.Wallets.SingleAsync(w => w.Id == wallet.Id);
    var baseline = debitWallet.MainBalance.Cents;
    Check(debitWallet.Debit(Money.FromCents(100), "concurrent debit").IsSuccess, "Debit rejected");
    creditWallet.Deposit(Money.FromCents(100), "concurrent credit");
    async Task<bool> Save(BillingDbContext db)
    {
        try { await new Sapphire.Billing.Infrastructure.Persistence.UnitOfWork(db).SaveChangesAsync(); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }
    var writes = await Task.WhenAll(Save(debitDb), Save(creditDb));
    Check(writes.Count(x => x) == 1, "Concurrent wallet writers both committed stale state");
    await using var verifyDb = new BillingDbContext(billingOptions);
    var finalBalance = (await verifyDb.Wallets.SingleAsync(w => w.Id == wallet.Id)).MainBalance.Cents;
    Check(finalBalance == baseline - 100 || finalBalance == baseline + 100,
        "Wallet balance does not match the committed write");
}
Console.WriteLine("Concurrent wallet debit/credit: stale writer rejected; no lost update.");

var promo = Promocode.Create("ONCE-" + Guid.NewGuid().ToString("N")[..8],
    PromocodeType.FixedAmount, 100, DateTime.UtcNow.AddMinutes(-1),
    DateTime.UtcNow.AddHours(1), maxTotalUses: 1, maxUsesPerUser: 1);
billing.ChangeTracker.Clear();
billing.Promocodes.Add(promo);
await billing.SaveChangesAsync();
async Task<bool> ApplyPromo()
{
    await using var db = new BillingDbContext(billingOptions);
    var handler = new ApplyPromocodeCommandHandler(new PromocodeRepository(db),
        new WalletRepository(db), new Sapphire.Billing.Infrastructure.Persistence.UnitOfWork(db));
    try
    {
        var result = await handler.Handle(new ApplyPromocodeCommand
            { WalletId = wallet.Id, PromocodeCode = promo.Code }, default);
        return result.IsSuccess;
    }
    catch (DbUpdateConcurrencyException) { return false; }
}
var promoOutcomes = await Task.WhenAll(ApplyPromo(), ApplyPromo());
Check(promoOutcomes.Count(x => x) == 1, "One-use promo did not have exactly one success");
await using (var verifyPromo = new BillingDbContext(billingOptions))
{
    Check((await verifyPromo.Promocodes.SingleAsync(p => p.Id == promo.Id)).UsedCount == 1,
        "Promo usage count is not one");
    Check((await verifyPromo.Wallets.SingleAsync(w => w.Id == wallet.Id)).BonusBalance.Cents == 100,
        "Promo bonus was credited more than once");
}
Console.WriteLine("Concurrent one-use promocode: exactly one bonus credit.");

var authOptions = new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(authConnection).Options;
var authUserId = Guid.NewGuid();
var authUser = User.CreateWithId(authUserId, "replay" + Guid.NewGuid().ToString("N")[..8],
    "replay" + Guid.NewGuid().ToString("N")[..8] + "@example.com", "hash", "salt");
var revoked = authUser.CreateRefreshToken("smoke-revoked-" + Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
var successor = authUser.CreateRefreshToken("smoke-successor-" + Guid.NewGuid(),
    DateTime.UtcNow.AddHours(1), familyId: revoked.FamilyId);
revoked.MarkAsReplaced(successor.Id);
await using (var seedAuth = new AuthDbContext(authOptions))
{
    seedAuth.Users.Add(authUser);
    await seedAuth.SaveChangesAsync();
}
await using (var replayDb = new AuthDbContext(authOptions))
{
    var handler = new RefreshTokenCommandHandler(new RefreshTokenRepository(replayDb),
        new UserRepository(replayDb), new ActivityHistoryRepository(replayDb),
        new Sapphire.Auth.Infrastructure.Persistence.UnitOfWork(replayDb), new SmokeTokenService(), null!);
    var response = await handler.Handle(new RefreshTokenCommand { RefreshToken = revoked.TokenHash }, default);
    Check(response.IsFailure, "Revoked refresh token was accepted");
}
await using (var verifyAuth = new AuthDbContext(authOptions))
{
    Check((await verifyAuth.RefreshTokens.SingleAsync(t => t.Id == successor.Id)).IsRevoked,
        "Refresh token replay did not revoke the successor family");
}
Console.WriteLine("Revoked refresh token replay: rejected and token family revoked.");

sealed class StartGateBillingClient(DbContextOptions<BillingDbContext> options) : IBillingReservationClient
{
    private int _arrived;
    private readonly TaskCompletionSource _both = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<Result> ReserveAsync(Guid sessionId, Guid userId, Guid entitlementId,
        DateTime startedAt, DateTime plannedEndAt, CancellationToken ct)
    {
        await using var db = new BillingDbContext(options);
        var result = await new BillingReservationService(db).ReserveAsync(
            new ReserveRequest(sessionId, userId, entitlementId, startedAt, plannedEndAt), ct);
        if (Interlocked.Increment(ref _arrived) == 2) _both.TrySetResult();
        await _both.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public async Task<Result> ReleaseAsync(Guid sessionId, CancellationToken ct)
    {
        await using var db = new BillingDbContext(options);
        return await new BillingReservationService(db).ReleaseAsync(sessionId, ct);
    }
}

sealed class SmokeTokenService : ITokenService
{
    public DateTime GetAccessTokenExpiresAt(string token) => throw new NotSupportedException();
    public string HashRefreshToken(string token) => token;
    public string GenerateAccessToken(User user) => throw new NotSupportedException();
    public (string Token, DateTime ExpiresAt) GenerateRefreshToken() => throw new NotSupportedException();
    public Guid? GetUserIdFromToken(string token) => throw new NotSupportedException();
    public Task<TokenDto> GenerateTokensAsync(User user, string? deviceInfo = null,
        string? ipAddress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
