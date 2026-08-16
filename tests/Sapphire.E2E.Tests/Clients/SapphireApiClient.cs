using System.Net.Http.Json;
using System.Text.Json;
using Sapphire.E2E.Tests.Models;

namespace Sapphire.E2E.Tests.Clients;

/// <summary>
/// Unified HTTP client for all Sapphire microservices.
/// Authenticated calls automatically attach the Bearer token.
/// </summary>
public sealed class SapphireApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _accessToken;
    private string? _refreshToken;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SapphireApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public SapphireApiClient(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public string? AccessToken => _accessToken;

    public void SetAuth(string accessToken, string? refreshToken = null)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
    }

    // ── Auth ──

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public async Task LogoutAsync()
    {
        var response = await _http.PostAsJsonAsync("/api/auth/logout",
            new { refreshToken = _refreshToken }, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    // ── Billing ──

    public async Task<PaymentResponse> TopUpAsync(long amountCents, string method = "cash")
    {
        var response = await _http.PostAsJsonAsync("/api/billing/topup",
            new TopUpRequest(amountCents, method), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions))!;
    }

    public async Task<WalletResponse> GetWalletAsync()
    {
        var response = await _http.GetAsync("/api/billing/wallet");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WalletResponse>(JsonOptions))!;
    }

    // ── Sessions ──

    public async Task<SessionResponse> StartSessionAsync(StartSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/sessions/start", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions))!;
    }

    public async Task<SessionResponse> EndSessionAsync(Guid sessionId)
    {
        var response = await _http.PostAsJsonAsync($"/api/sessions/{sessionId}/end",
            new { sessionId }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions))!;
    }

    public async Task<SessionResponse?> GetSessionAsync(Guid sessionId)
    {
        var response = await _http.GetAsync($"/api/sessions/{sessionId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
    }

    // ── Tariffs ──

    public async Task<List<TariffDto>> GetTariffsAsync()
    {
        var response = await _http.GetAsync("/api/billing/tariffs");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<TariffDto>>(JsonOptions))!;
    }

    public void Dispose() => _http.Dispose();
}
