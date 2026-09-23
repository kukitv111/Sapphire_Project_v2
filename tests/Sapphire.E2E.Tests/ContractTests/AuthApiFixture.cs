using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sapphire.Auth.Api;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.E2E.Tests.ContractTests;

/// <summary>
/// Boots the real Auth API (Program.cs) with an in-memory EF provider
/// and exercises the HTTP contract end to end (routing, auth pipeline, MediatR, persistence).
/// </summary>
public sealed class AuthApiFixture : WebApplicationFactory<Sapphire.Auth.Api.Controllers.AuthController>
{
    private readonly string _databaseName = $"auth-contract-{Guid.NewGuid()}";

    public string MintToken(Guid userId, string email, params string[] roles)
        => Services.GetRequiredService<TokenService>()
            .GenerateAccessToken(userId, email, roles, Array.Empty<string>());

    public string GetDatabaseName() => _databaseName;

    public async Task<int> GetOutboxMessageCountAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await dbContext.OutboxMessages.CountAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:SecretKey", "contract-test-secret-at-least-32-characters-long");
        builder.UseSetting("Jwt:Issuer", "sapphire-auth");
        builder.UseSetting("Jwt:Audience", "sapphire-clients");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AuthDbContext>>();

            var npgsqlExtension = typeof(NpgsqlDbContextOptionsBuilderExtensions).Assembly;
            foreach (var descriptor in services
                         .Where(d => d.ImplementationType?.Assembly == npgsqlExtension)
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
