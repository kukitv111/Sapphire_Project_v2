using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sapphire.Shared.Security.Jwt;
using Xunit;

namespace Sapphire.E2E.Tests;

public sealed class SecurityTests
{
    private const string TestSecret = "test-secret-32-chars-placeholder-ok!!";

    [Fact]
    public async Task Session_StartSession_Unauthenticated_Returns401()
    {
        using var app = await CreateSecurityTestAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            computerId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            startTime = DateTime.UtcNow,
            endTime = DateTime.UtcNow.AddMinutes(30)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Billing_CreateTariff_Unauthenticated_Returns401()
    {
        using var app = await CreateSecurityTestAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/billing/tariffs", new { name = "Standard" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Billing_CreateTariff_AuthenticatedWithoutAdminRole_Returns403()
    {
        using var app = await CreateSecurityTestAppAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            BuildToken(Guid.NewGuid(), roles: ["User"]));

        var response = await client.PostAsJsonAsync("/api/billing/tariffs", new { name = "Standard" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Billing_CreateTariff_AuthenticatedWithAdminRole_ReachesApplicationLayer()
    {
        using var app = await CreateSecurityTestAppAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            BuildToken(Guid.NewGuid(), roles: ["Admin"]));

        var response = await client.PostAsJsonAsync("/api/billing/tariffs", new { name = "Standard" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Session_StartSession_UsesClaimUserId_NotCallerSuppliedUserId()
    {
        using var app = await CreateSecurityTestAppAsync();
        using var client = app.GetTestClient();
        var authenticatedUserId = Guid.NewGuid();
        var callerSuppliedUserId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            BuildToken(authenticatedUserId, roles: ["User"]));

        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            computerId = Guid.NewGuid(),
            userId = callerSuppliedUserId,
            startTime = DateTime.UtcNow,
            endTime = DateTime.UtcNow.AddMinutes(30)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SessionIdentityResponse>();
        payload.Should().NotBeNull();
        payload!.ResolvedUserId.Should().Be(authenticatedUserId);
        payload.CallerSuppliedUserId.Should().Be(callerSuppliedUserId);
    }

    [Fact]
    public void Production_MissingSecret_IsRejected()
    {
        var act = () => JwtOptionsValidator.Validate(null, "Production");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Production_EmptySecret_IsRejected()
    {
        var act = () => JwtOptionsValidator.Validate(new JwtOptions { SecretKey = "  " }, "Production");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Production_DefaultSecret_IsRejected()
    {
        var act = () => JwtOptionsValidator.Validate(
            new JwtOptions { SecretKey = "sapphire-dev-secret-key-change-me-in-production-32chars-min" },
            "Production");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Development_ExplicitNonDefaultSecret_IsAccepted()
    {
        var options = new JwtOptions 
        { 
            SecretKey = TestSecret, 
            Issuer = "test", 
            Audience = "test", 
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 30
        };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().NotThrow();
    }

    private static async Task<WebApplication> CreateSecurityTestAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test",
                    ValidAudience = "test",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/api/sessions", (ClaimsPrincipal user, StartSessionRequest request) =>
        {
            var resolved = Guid.Parse(user.FindFirstValue("sub")!);
            return Results.Ok(new SessionIdentityResponse(resolved, request.UserId));
        }).RequireAuthorization();

        app.MapPost("/api/billing/tariffs", () => Results.Ok()).RequireAuthorization("AdminOnly");
        app.MapPost("/api/auth/change-password", () => Results.Ok()).RequireAuthorization();

        await app.StartAsync();
        return app;
    }

    private static string BuildToken(Guid userId, string[] roles)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("email", "user@test.local")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record StartSessionRequest(Guid ComputerId, Guid UserId, DateTime StartTime, DateTime EndTime);
    private sealed record SessionIdentityResponse(Guid ResolvedUserId, Guid CallerSuppliedUserId);
}
