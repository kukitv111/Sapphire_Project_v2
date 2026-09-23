using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Domain.Events;
using Sapphire.Billing.Api.Controllers;
using Sapphire.Session.Api.Controllers;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Session.Infrastructure.Persistence;
using Sapphire.Shared.Kernel.Events;
using Sapphire.Shared.Messaging.Outbox;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.E2E.Tests;

public sealed class ProductionRegressionTests
{
    private sealed class ApiFactory<T> : WebApplicationFactory<T> where T : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SecretKey", "regression-tests-secret-at-least-32-characters");
            builder.UseSetting("Jwt:Issuer", "sapphire-auth");
            builder.UseSetting("Jwt:Audience", "sapphire-clients");
        }
        public HttpClient AuthenticatedClient(string role = "User")
        {
            var client = CreateClient();
            var token = Services.GetRequiredService<TokenService>()
                .GenerateAccessToken(Guid.NewGuid(), "test@example.com", [role], []);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            return client;
        }
    }

    [Fact]
    public void Outbox_preserves_concrete_event_type_and_payload()
    {
        IDomainEvent domainEvent = new UserRegisteredEvent { UserId = Guid.NewGuid(), Username = "alice" };
        var message = OutboxMessage.Create(domainEvent);
        Assert.Equal(typeof(UserRegisteredEvent).AssemblyQualifiedName, message.Type);
        Assert.Equal("alice", message.Deserialize<UserRegisteredEvent>()!.Username);
        Assert.Equal(((UserRegisteredEvent)domainEvent).UserId, message.Deserialize<UserRegisteredEvent>()!.UserId);
    }

    [Fact]
    public async Task Billing_rejects_access_to_another_users_wallet()
    {
        using var factory = new ApiFactory<BillingController>();
        using var client = factory.AuthenticatedClient();
        var response = await client.GetAsync($"/api/billing/users/{Guid.NewGuid()}/wallet");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Billing_rejects_unprivileged_mutations_before_database_access()
    {
        using var factory = new ApiFactory<BillingController>();
        using var client = factory.AuthenticatedClient();
        foreach (var path in new[] { "/api/billing/tariffs", $"/api/billing/wallets/{Guid.NewGuid()}/promocodes",
            $"/api/billing/users/{Guid.NewGuid()}/tariffs" })
        {
            var response = await client.PostAsJsonAsync(path, new { });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Session_list_requires_staff_role()
    {
        using var factory = new ApiFactory<SessionController>();
        using var client = factory.AuthenticatedClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/sessions")).StatusCode);
    }

    private sealed class FailOnce : SaveChangesInterceptor
    {
        public bool Fail { get; set; } = true;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail) { Fail = false; throw new InvalidOperationException("simulated write failure"); }
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task Failed_save_preserves_events_and_retry_does_not_duplicate_outbox()
    {
        var interceptor = new FailOnce();
        await using var db = new SessionDbContext(new DbContextOptionsBuilder<SessionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(interceptor).Options);
        var computer = new Computer("test");
        db.Computers.Add(computer);
        var unitOfWork = new UnitOfWork(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.SaveChangesAsync());
        Assert.Single(computer.DomainEvents);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        await unitOfWork.SaveChangesAsync();
        Assert.Empty(computer.DomainEvents);
        Assert.Equal(1, await db.OutboxMessages.CountAsync());
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(1, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public void Production_rejects_repository_jwt_placeholder()
    {
        Assert.Throws<InvalidOperationException>(() => JwtOptionsValidator.Validate(new JwtOptions
        {
            SecretKey = "SET-VIA-ENV-VAR-OR-USER-SECRETS-JWT__SECRETKEY",
            Issuer = "test", Audience = "test"
        }, "Production"));
    }
    [Fact]
    public async Task Password_change_ignores_forged_body_identity()
    {
        using var factory = new ContractTests.AuthApiFixture();
        using var client = factory.CreateClient();
        async Task<JsonElement> Register()
        {
            var name = $"test_{Guid.NewGuid():N}"[..24];
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            { username = name, email = $"{name}@example.com", password = "Password123!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("value");
        }
        var caller = await Register();
        var victim = await Register();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            caller.GetProperty("tokens").GetProperty("accessToken").GetString());
        var change = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            userId = victim.GetProperty("user").GetProperty("id").GetGuid(),
            currentPassword = "Password123!", newPassword = "Changed123!"
        });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        var victimLogin = await client.PostAsJsonAsync("/api/auth/login", new
        { login = victim.GetProperty("user").GetProperty("username").GetString(), password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, victimLogin.StatusCode);
        var callerOldPassword = await client.PostAsJsonAsync("/api/auth/login", new
        { login = caller.GetProperty("user").GetProperty("username").GetString(), password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, callerOldPassword.StatusCode);
        var callerNewPassword = await client.PostAsJsonAsync("/api/auth/login", new
        { login = caller.GetProperty("user").GetProperty("username").GetString(), password = "Changed123!" });
        Assert.Equal(HttpStatusCode.OK, callerNewPassword.StatusCode);
    }

    [Fact]
    public async Task Real_auth_host_limits_login_attempts()
    {
        using var factory = new ContractTests.AuthApiFixture();
        using var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
    }

    [Fact]
    public void Money_addition_rejects_overflow_and_percentage_discount_does_not_wrap()
    {
        Assert.Throws<OverflowException>(() => Sapphire.Shared.Kernel.ValueObjects.Money
            .FromCents(long.MaxValue).Add(Sapphire.Shared.Kernel.ValueObjects.Money.FromCents(1)));
        Assert.Equal(0, Sapphire.Billing.Domain.ValueObjects.Discount.Percent(100).ApplyTo(long.MaxValue));
    }

    [Fact]
    public void Postgres_login_query_translates_converted_value_objects()
    {
        using var db = new Sapphire.Auth.Infrastructure.Persistence.AuthDbContext(
            new DbContextOptionsBuilder<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>()
                .UseNpgsql("Host=localhost;Database=model_test;Username=test").Options);
        var username = Sapphire.Auth.Domain.ValueObjects.Username.From("ALICE");
        var email = Sapphire.Shared.Kernel.ValueObjects.Email.From("Alice@example.com");
        Assert.Contains("WHERE", db.Users.Where(u => u.Username == username).ToQueryString());
        Assert.Contains("WHERE", db.Users.Where(u => u.Email == email).ToQueryString());
    }

    [Fact]
    public async Task Promocode_repository_loads_usage_limits_after_context_reload()
    {
        await using var db = new Sapphire.Billing.Infrastructure.Persistence.BillingDbContext(
            new DbContextOptionsBuilder<Sapphire.Billing.Infrastructure.Persistence.BillingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var userId = Guid.NewGuid();
        var promo = Sapphire.Billing.Domain.Aggregates.Promocode.Create("ONCE",
            Sapphire.Billing.Domain.Enums.PromocodeType.FixedAmount, 100,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), maxUsesPerUser: 1);
        promo.RecordUsage(userId, 100);
        db.Promocodes.Add(promo);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new Sapphire.Billing.Infrastructure.Persistence.Repositories.PromocodeRepository(db);
        var loaded = await repository.GetByCodeAsync("once");
        Assert.NotNull(loaded);
        Assert.False(loaded.CanBeUsed(userId, DateTime.UtcNow));
        Assert.True(loaded.CanBeUsed(Guid.NewGuid(), DateTime.UtcNow));
    }

}
