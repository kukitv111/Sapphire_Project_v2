using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Auth.Infrastructure.Security;
using Sapphire.Auth.Domain.ValueObjects;

namespace Sapphire.E2E.Tests.ContractTests;

public sealed class TokenRevocationTests
{
    [Fact]
    public async Task Banned_user_cannot_continue_with_old_access_token()
    {
        using var app = new AuthApiFixture(enforceTokenVersion: true);
        var userId = Guid.NewGuid();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Users.Add(User.CreateWithId(userId, "bannedtest", "banned@test.local", "hash", "salt"));
            await db.SaveChangesAsync();
        }
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            app.MintToken(userId, "banned@test.local", "User"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.Ban("test ban", Guid.NewGuid());
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Initial_password_change_blocks_normal_access_then_revokes_old_token()
    {
        using var app = new AuthApiFixture(enforceTokenVersion: true);
        var (hash, salt) = new PasswordHasher().HashPassword("InitialPass9!");
        var user = User.Create("firstlogin", "firstlogin@test.local", Password.FromHash(hash, salt));
        user.RequirePasswordChange();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            app.MintToken(user.Id, user.Email.Value, "Admin"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var changed = await client.PostAsJsonAsync("/api/auth/change-password",
            new { CurrentPassword = "InitialPass9!", NewPassword = "ChangedPass9!" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var verify = app.Services.CreateScope();
        var current = await verify.ServiceProvider.GetRequiredService<AuthDbContext>()
            .Users.SingleAsync(u => u.Id == user.Id);
        Assert.False(current.MustChangePassword);
        Assert.Equal(1, current.TokenVersion);
    }
}
