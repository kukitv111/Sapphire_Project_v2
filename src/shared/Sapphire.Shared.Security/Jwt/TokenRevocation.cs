using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Sapphire.Shared.Security.Jwt;

public sealed record TokenVersionState(long Version, bool Active, bool MustChangePassword);

public interface ITokenRevocationService
{
    Task<TokenVersionState?> GetAsync(Guid userId, CancellationToken ct);
}

// Billing and Session fail closed if Auth cannot answer a revocation check.
public sealed class RemoteTokenRevocationService(HttpClient client, IConfiguration configuration)
    : ITokenRevocationService
{
    public async Task<TokenVersionState?> GetAsync(Guid userId, CancellationToken ct)
    {
        var baseUrl = configuration["Auth:InternalUrl"]
            ?? throw new InvalidOperationException("Auth:InternalUrl is required");
        var secret = configuration["Messaging:SharedSecret"]
            ?? throw new InvalidOperationException("Messaging:SharedSecret is required");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            baseUrl.TrimEnd('/') + "/internal/token-version/" + userId);
        request.Headers.Add("X-Sapphire-Message-Key", secret);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TokenVersionState>(cancellationToken: ct);
    }
}
