using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Sapphire.E2E.Tests.Fixtures;

/// <summary>
/// In-process test fixture with WireMock servers for each microservice.
/// Tests run entirely in memory — no Docker needed.
/// </summary>
public sealed class E2ETestFixture : IAsyncLifetime
{
    public WireMockServer AuthServer { get; private set; } = null!;
    public WireMockServer BillingServer { get; private set; } = null!;
    public WireMockServer SessionServer { get; private set; } = null!;

    public string AuthBaseUrl => AuthServer.Urls[0];
    public string BillingBaseUrl => BillingServer.Urls[0];
    public string SessionBaseUrl => SessionServer.Urls[0];

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task InitializeAsync()
    {
        AuthServer = WireMockServer.Start();
        BillingServer = WireMockServer.Start();
        SessionServer = WireMockServer.Start();

        SetupAuthMock();
        SetupBillingMock();
        SetupSessionMock();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        AuthServer.Stop();
        BillingServer.Stop();
        SessionServer.Stop();
        AuthServer.Dispose();
        BillingServer.Dispose();
        SessionServer.Dispose();
        await Task.CompletedTask;
    }

    private static (string Username, string Email) ParseRequestBody(string? body)
    {
        if (string.IsNullOrEmpty(body)) return ("unknown_user", "unknown@test.com");
        try
        {
            var doc = JsonDocument.Parse(body);
            var username = doc.RootElement.TryGetProperty("username", out var u) ? u.GetString() ?? "unknown_user" : "unknown_user";
            var email = doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? "unknown@test.com" : "unknown@test.com";
            var login = doc.RootElement.TryGetProperty("login", out var l) ? l.GetString() : null;
            return (username ?? login ?? "unknown_user", email);
        }
        catch { return ("unknown_user", "unknown@test.com"); }
    }

    private void SetupAuthMock()
    {
        AuthServer.Given(Request.Create().WithPath("/api/auth/register").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ctx =>
                {
                    var id = Guid.NewGuid();
                    var (username, email) = ParseRequestBody(ctx.Body);
                    return JsonSerializer.Serialize(new
                    {
                        accessToken = GenerateFakeJwt(id),
                        refreshToken = "refresh_" + Guid.NewGuid().ToString("N"),
                        expiresAt = DateTime.UtcNow.AddMinutes(15),
                        user = BuildUser(id, username, email)
                    }, Json);
                }));

        AuthServer.Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ctx =>
                {
                    var id = Guid.NewGuid();
                    var (_, email) = ParseRequestBody(ctx.Body);
                    var login = "unknown_user";
                    if (!string.IsNullOrEmpty(ctx.Body))
                    {
                        try
                        {
                            var doc = JsonDocument.Parse(ctx.Body);
                            if (doc.RootElement.TryGetProperty("login", out var l))
                                login = l.GetString() ?? "unknown_user";
                        }
                        catch { }
                    }
                    return JsonSerializer.Serialize(new
                    {
                        accessToken = GenerateFakeJwt(id),
                        refreshToken = "refresh_" + Guid.NewGuid().ToString("N"),
                        expiresAt = DateTime.UtcNow.AddMinutes(15),
                        user = BuildUser(id, login, email)
                    }, Json);
                }));

        AuthServer.Given(Request.Create().WithPath("/api/auth/refresh").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ =>
                {
                    var id = Guid.NewGuid();
                    return JsonSerializer.Serialize(new
                    {
                        accessToken = GenerateFakeJwt(id),
                        refreshToken = "refresh_" + Guid.NewGuid().ToString("N"),
                        expiresAt = DateTime.UtcNow.AddMinutes(15),
                        user = BuildUser(id, "refreshed_user", "refreshed@test.com")
                    }, Json);
                }));

        AuthServer.Given(Request.Create().WithPath("/api/auth/logout").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
    }

    private void SetupBillingMock()
    {
        BillingServer.Given(Request.Create().WithPath("/api/billing/tariffs").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ => JsonSerializer.Serialize(new[]
                {
                    new { id = Guid.NewGuid(), name = "Standard", type = "PerMinute",
                          pricePerMinute = 2.50m, pricePerHour = 150m, isActive = true }
                }, Json)));

        BillingServer.Given(Request.Create().WithPath("/api/billing/topup").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ => JsonSerializer.Serialize(new
                {
                    id = Guid.NewGuid(),
                    userId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    amountCents = 10000L,
                    method = "cash",
                    status = "Completed",
                    type = "topup",
                    createdAt = DateTime.UtcNow
                }, Json)));

        BillingServer.Given(Request.Create().WithPath("/api/billing/deduct").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ => JsonSerializer.Serialize(new { success = true }, Json)));

        BillingServer.Given(Request.Create().WithPath("/api/billing/wallet").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ => JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    userId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    balanceCents = 5000L,
                    bonusBalanceCents = 0L
                }, Json)));
    }

    private void SetupSessionMock()
    {
        var sessions = new Dictionary<Guid, (Guid ComputerId, Guid UserId, Guid TariffId, DateTime Start, string Status, decimal Rate)>();

        SessionServer.Given(Request.Create().WithPath("/api/sessions/computers").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ => JsonSerializer.Serialize(new[]
                {
                    new { id = Guid.NewGuid(), name = "PC-01", ipAddress = "192.168.1.101", status = "Available" }
                }, Json)));

        SessionServer.Given(Request.Create().WithPath("/api/sessions/start").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ =>
                {
                    var sid = Guid.NewGuid();
                    var start = DateTime.UtcNow;
                    sessions[sid] = (Guid.NewGuid(), Guid.Parse("00000000-0000-0000-0000-000000000001"),
                                     Guid.NewGuid(), start, "Active", 2.50m);
                    return JsonSerializer.Serialize(new
                    {
                        id = sid,
                        computerId = sessions[sid].ComputerId,
                        userId = sessions[sid].UserId,
                        tariffId = sessions[sid].TariffId,
                        startTime = start,
                        endTime = (DateTime?)null,
                        status = "Active",
                        totalCost = 0m
                    }, Json);
                }));

        SessionServer.Given(Request.Create().WithPath("/api/sessions/*/end").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ =>
                {
                    var entry = sessions.LastOrDefault();
                    if (entry.Value == default) return "{\"error\":\"not found\"}";

                    var sid = entry.Key;
                    var s = entry.Value;
                    var end = DateTime.UtcNow;
                    var minutes = Math.Max(1, (int)(end - s.Start).TotalMinutes);
                    var cost = s.Rate * minutes;
                    sessions[sid] = s with { Status = "Ended" };

                    return JsonSerializer.Serialize(new
                    {
                        id = sid,
                        computerId = s.ComputerId,
                        userId = s.UserId,
                        tariffId = s.TariffId,
                        startTime = s.Start,
                        endTime = end,
                        status = "Ended",
                        totalCost = cost
                    }, Json);
                }));

        SessionServer.Given(Request.Create().WithPath("/api/sessions/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_ =>
                {
                    var entry = sessions.LastOrDefault();
                    if (entry.Value == default) return "{\"error\":\"not found\"}";
                    var s = entry.Value;
                    return JsonSerializer.Serialize(new
                    {
                        id = entry.Key,
                        computerId = s.ComputerId,
                        userId = s.UserId,
                        tariffId = s.TariffId,
                        startTime = s.Start,
                        endTime = (DateTime?)null,
                        status = s.Status,
                        totalCost = 0m
                    }, Json);
                }));
    }

    private static string GenerateFakeJwt(Guid userId)
    {
        var header = Convert.ToBase64String("{\"alg\":\"HS256\",\"typ\":\"JWT\""u8.ToArray());
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = userId.ToString(),
            email = "test@test.com",
            exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        }, Json));
        var sig = Convert.ToBase64String("fake_sig"u8.ToArray());
        return $"{header}.{payload}.{sig}";
    }

    private static object BuildUser(Guid id, string username, string email) => new
    {
        id, username, email,
        phone = (string?)null,
        status = "Active",
        isBanned = false,
        roles = Array.Empty<object>()
    };
}
