using FluentAssertions;
using Sapphire.E2E.Tests.Clients;
using Sapphire.E2E.Tests.Fixtures;
using Sapphire.E2E.Tests.Models;
using Xunit;

namespace Sapphire.E2E.Tests;

/// <summary>
/// End-to-End integration tests covering the complete user journey
/// through Auth, Billing, and Session microservices.
/// </summary>
public sealed class EndToEndTests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public EndToEndTests(E2ETestFixture fixture) => _fixture = fixture;

    private SapphireApiClient CreateClient(string baseUrl) => new(baseUrl);

    [Fact]
    public async Task FullLifecycle_RegisterTopUpStartSession_EndSession()
    {
        using var authClient = CreateClient(_fixture.AuthBaseUrl);
        using var billingClient = CreateClient(_fixture.BillingBaseUrl);
        using var sessionClient = CreateClient(_fixture.SessionBaseUrl);

        var username = $"e2e_user_{Guid.NewGuid():N}";
        var email = $"{username}@sapphire.test";

        // Step 1: Register
        var registerResponse = await authClient.RegisterAsync(new RegisterRequest(
            Username: username, Email: email, Password: "SecureP@ss123"));

        registerResponse.Should().NotBeNull();
        registerResponse.User.Should().NotBeNull();
        registerResponse.User.Username.Should().Be(username);
        registerResponse.User.Email.Should().Be(email);
        registerResponse.User.Status.Should().Be("Active");
        registerResponse.AccessToken.Should().NotBeNullOrEmpty();
        registerResponse.RefreshToken.Should().NotBeNullOrEmpty();

        // Set auth token for subsequent requests
        authClient.SetAuth(registerResponse.AccessToken, registerResponse.RefreshToken);
        billingClient.SetAuth(registerResponse.AccessToken, registerResponse.RefreshToken);
        sessionClient.SetAuth(registerResponse.AccessToken, registerResponse.RefreshToken);

        // Step 2: Login
        var loginResponse = await authClient.LoginAsync(new LoginRequest(
            Login: username, Password: "SecureP@ss123"));

        loginResponse.Should().NotBeNull();
        loginResponse.AccessToken.Should().NotBeNullOrEmpty();

        // Step 3: Top up wallet
        const long topUpAmount = 10_000;
        var payment = await billingClient.TopUpAsync(topUpAmount);

        payment.Should().NotBeNull();
        payment.AmountCents.Should().Be(topUpAmount);
        payment.Status.Should().Be("Completed");
        payment.Type.Should().Be("topup");

        // Step 4: Get tariffs
        var tariffs = await billingClient.GetTariffsAsync();
        tariffs.Should().NotBeEmpty();

        var standardTariff = tariffs.First(t => t.Name == "Standard");
        standardTariff.PricePerMinute.Should().Be(2.50m);

        // Step 5: Start session
        var startResponse = await sessionClient.StartSessionAsync(new StartSessionRequest(
            ComputerId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            TariffId: standardTariff.Id));

        startResponse.Should().NotBeNull();
        startResponse.Id.Should().NotBe(Guid.Empty);
        startResponse.Status.Should().Be("Active");
        startResponse.TotalCost.Should().Be(0m);

        // Step 6: Wait (simulates gaming session)
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Step 7: End session
        var endResponse = await sessionClient.EndSessionAsync(startResponse.Id);

        endResponse.Should().NotBeNull();
        endResponse.Status.Should().Be("Ended");
        endResponse.EndTime.Should().NotBeNull();
        endResponse.EndTime!.Value.Should().BeAfter(startResponse.StartTime);
        endResponse.TotalCost.Should().BeGreaterThan(0m);

        // Step 8: Verify wallet balance
        var wallet = await billingClient.GetWalletAsync();
        wallet.Should().NotBeNull();
        wallet.BalanceCents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Session_CostIsCalculatedCorrectly()
    {
        using var authClient = CreateClient(_fixture.AuthBaseUrl);
        using var billingClient = CreateClient(_fixture.BillingBaseUrl);
        using var sessionClient = CreateClient(_fixture.SessionBaseUrl);

        var reg = await authClient.RegisterAsync(new RegisterRequest(
            Username: $"e2e_cost_{Guid.NewGuid():N}",
            Email: $"cost_{Guid.NewGuid():N}@sapphire.test",
            Password: "SecureP@ss123"));
        sessionClient.SetAuth(reg.AccessToken, reg.RefreshToken);

        var tariffs = await billingClient.GetTariffsAsync();
        var tariff = tariffs.First();

        var session = await sessionClient.StartSessionAsync(new StartSessionRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            tariff.Id));

        await Task.Delay(TimeSpan.FromSeconds(1));

        var ended = await sessionClient.EndSessionAsync(session.Id);

        // Cost = rate * ceil(minutes). Minimum billable is 1 minute → 2.50
        ended.TotalCost.Should().Be(tariff.PricePerMinute);
    }

    [Fact]
    public async Task Auth_RefreshToken_GeneratesNewAccessToken()
    {
        using var authClient = CreateClient(_fixture.AuthBaseUrl);

        var reg = await authClient.RegisterAsync(new RegisterRequest(
            Username: $"e2e_refresh_{Guid.NewGuid():N}",
            Email: $"refresh_{Guid.NewGuid():N}@sapphire.test",
            Password: "SecureP@ss123"));

        reg.RefreshToken.Should().NotBeNullOrEmpty();

        var refreshed = await authClient.RefreshTokenAsync(reg.RefreshToken);
        refreshed.Should().NotBeNull();
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.AccessToken.Should().NotBe(reg.AccessToken, "refresh should produce a new token");
    }

    [Fact]
    public async Task Billing_GetWallet_ReturnsValidBalance()
    {
        using var authClient = CreateClient(_fixture.AuthBaseUrl);
        using var billingClient = CreateClient(_fixture.BillingBaseUrl);

        var reg = await authClient.RegisterAsync(new RegisterRequest(
            Username: $"e2e_wallet_{Guid.NewGuid():N}",
            Email: $"wallet_{Guid.NewGuid():N}@sapphire.test",
            Password: "SecureP@ss123"));
        billingClient.SetAuth(reg.AccessToken, reg.RefreshToken);

        var wallet = await billingClient.GetWalletAsync();
        wallet.Should().NotBeNull();
        wallet.BalanceCents.Should().BeGreaterThanOrEqualTo(0);
        wallet.BonusBalanceCents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task FullLifecycle_MultipleSessions_AccumulatedCharges()
    {
        using var authClient = CreateClient(_fixture.AuthBaseUrl);
        using var billingClient = CreateClient(_fixture.BillingBaseUrl);
        using var sessionClient = CreateClient(_fixture.SessionBaseUrl);

        var reg = await authClient.RegisterAsync(new RegisterRequest(
            Username: $"e2e_multi_{Guid.NewGuid():N}",
            Email: $"multi_{Guid.NewGuid():N}@sapphire.test",
            Password: "SecureP@ss123"));
        sessionClient.SetAuth(reg.AccessToken, reg.RefreshToken);

        var tariffs = await billingClient.GetTariffsAsync();
        var tariff = tariffs.First();

        for (int i = 0; i < 2; i++)
        {
            var session = await sessionClient.StartSessionAsync(new StartSessionRequest(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                tariff.Id));
            session.Status.Should().Be("Active");

            var ended = await sessionClient.EndSessionAsync(session.Id);
            ended.Status.Should().Be("Ended");
            ended.TotalCost.Should().BeGreaterThan(0);
        }
    }
}
