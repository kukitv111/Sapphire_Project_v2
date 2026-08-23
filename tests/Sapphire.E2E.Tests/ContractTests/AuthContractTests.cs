using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Sapphire.E2E.Tests.ContractTests;
using Xunit;

namespace Sapphire.E2E.Tests.ContractTests;

public sealed class AuthContractTests : IClassFixture<AuthApiFixture>
{
    private readonly AuthApiFixture _factory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public AuthContractTests(AuthApiFixture factory) => _factory = factory;

    private HttpClient Client() => _factory.CreateClient();

    [Fact]
    public async Task Register_then_login_returns_result_envelope()
    {
        var username = $"user_{Guid.NewGuid():N}";
        var register = await Client().PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@test.com",
            password = "Password123!"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await register.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var login = await Client().PostAsJsonAsync("/api/auth/login", new { login = username, password = "Password123!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        loginBody.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        loginBody.GetProperty("value").GetProperty("tokens").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Users_endpoint_requires_authentication()
    {
        var response = await Client().GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_endpoint_forbids_non_admin_role()
    {
        var client = Client();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", _factory.MintToken(Guid.NewGuid(), "user@test.com", "User"));
        var response = await client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_endpoint_returns_registered_user_for_admin()
    {
        var username = $"admin_{Guid.NewGuid():N}";
        var register = await Client().PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@test.com",
            password = "Password123!"
        });
        var regBody = await register.Content.ReadFromJsonAsync<JsonElement>(Json);
        regBody.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        var userId = regBody.GetProperty("value").GetProperty("user").GetProperty("id").GetGuid();

        var client = Client();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", _factory.MintToken(Guid.NewGuid(), "admin@test.com", "Admin"));
        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        var users = body.GetProperty("value").EnumerateArray().ToList();
        users.Should().Contain(u => u.GetProperty("id").GetGuid() == userId);
    }

    [Fact]
    public async Task Me_endpoint_returns_current_user()
    {
        var username = $"me_{Guid.NewGuid():N}";
        var register = await Client().PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@test.com",
            password = "Password123!"
        });
        var regBody = await register.Content.ReadFromJsonAsync<JsonElement>(Json);
        var userId = regBody.GetProperty("value").GetProperty("user").GetProperty("id").GetGuid();
        var accessToken = regBody.GetProperty("value").GetProperty("tokens").GetProperty("accessToken").GetString();

        var client = Client();
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        body.GetProperty("value").GetProperty("id").GetGuid().Should().Be(userId);
    }
}
