using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Api;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.E2E.Tests.ContractTests;

/// <summary>
/// Boots the real Auth API (Program.cs) with an in-memory EF provider
/// and exercises the HTTP contract end to end (routing, auth pipeline, MediatR, persistence).
/// </summary>
public sealed class AuthApiFixture : WebApplicationFactory<Program>
{
    public string MintToken(Guid userId, string email, params string[] roles)
        => Services.GetRequiredService<TokenService>()
            .GenerateAccessToken(userId, email, roles, Array.Empty<string>());

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase("auth-contract-tests"));
        });
    }
}
