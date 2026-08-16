using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Sapphire.E2E.Tests;

public sealed class SecurityTests : IClassFixture<SecurityTests.Fixture>
{
    public sealed class Fixture : IAsyncLifetime
    {
        public HttpClient Auth { get; private set; } = null!;
        public HttpClient Billing { get; private set; } = null!;
        public HttpClient Session { get; private set; } = null!;
        public string DevSecret { get; } = "dev-secret-32-chars-placeholder-ok!!";

        public async Task InitializeAsync()
        {
            // Minimal in-process hosts to validate middleware behaviour without full DB.
            Auth = BuildHost("/auth", DevSecret, cfg => { }).CreateClient();
            Billing = BuildHost("/billing", DevSecret, cfg => { }).CreateClient();
            Session = BuildHost("/session", DevSecret, cfg => { }).CreateClient();
            await Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            Auth?.Dispose();
            Billing?.Dispose();
            Session?.Dispose();
            return Task.CompletedTask;
        }

        private static WebApplicationFactory<Program> BuildHost(string routePrefix, string secret, Action<IDictionary<string, string?>> configure)
        {
            var config = new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = secret,
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };
            configure(config);
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b =>
                {
                    b.UseContentRoot(Directory.GetCurrentDirectory());
                    b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(config));
                });
        }

        public static string BuildToken(string secret, Guid userId, string[] roles, string[] permissions)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var claims = new List<System.Security.Claims.Claim>
            {
                new("sub", userId.ToString()),
                new("email", "user@test.com")
            };
            foreach (var r in roles) { claims.Add(new("role", r)); claims.Add(new(System.Security.Claims.ClaimTypes.Role, r)); }
            foreach (var p in permissions) claims.Add(new("permission", p));

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "test",
                audience: "test",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    private readonly Fixture _fx;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public SecurityTests(Fixture fx) => _fx = fx;

    [Fact]
    public async Task Billing_CreateTariff_Unauthenticated_Returns401()
    {
        var response = await _fx.Billing.PostAsJsonAsync("/api/billing/tariffs", new { name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_StartSession_Unauthenticated_Returns401()
    {
        var response = await _fx.Session.PostAsJsonAsync("/api/sessions/start", new { computerId = Guid.NewGuid(), userId = Guid.NewGuid(), startTime = DateTime.UtcNow, endTime = DateTime.UtcNow.AddMinutes(1) });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Billing_CreateTariff_ForbiddenRole_Returns403()
    {
        var token = Fixture.BuildToken(_fx.DevSecret, Guid.NewGuid(), roles: ["User"], permissions: Array.Empty<string>());
        _fx.Billing.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _fx.Billing.PostAsJsonAsync("/api/billing/tariffs", new { name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Billing_Wallet_BlockedForOtherUser()
    {
        var token = Fixture.BuildToken(_fx.DevSecret, Guid.NewGuid(), roles: ["User"], permissions: Array.Empty<string>());
        _fx.Billing.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var otherUserId = Guid.NewGuid();
        var response = await _fx.Billing.GetAsync($"/api/billing/wallets/{otherUserId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Session_StartSession_AllowedForAuthenticatedUser()
    {
        var token = Fixture.BuildToken(_fx.DevSecret, Guid.NewGuid(), roles: ["User"], permissions: Array.Empty<string>());
        _fx.Session.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _fx.Session.PostAsJsonAsync("/api/sessions/start", new { computerId = Guid.NewGuid(), userId = Guid.NewGuid(), startTime = DateTime.UtcNow, endTime = DateTime.UtcNow.AddMinutes(1) });
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProductionStartup_DefaultSecret_Rejected()
    {
        var response = await BuildProductionAppWithSecret("change-me-in-production!!!").Host.StartAsync();
        // Expect failure when starting with default dev secret in production.
        await response.Should().ThrowAsync<InvalidOperationException>();
    }

    private WebApplicationFactory<Program> BuildProductionAppWithSecret(string secret)
    {
        var config = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Jwt:SecretKey"] = secret,
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseContentRoot(Directory.GetCurrentDirectory());
                b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(config));
            });
    }
}
