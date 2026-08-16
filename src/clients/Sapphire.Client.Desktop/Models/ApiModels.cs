namespace Sapphire.Client.Desktop.Models;

public record AuthResponse(AccessToken Tokens, UserDto User);
public record AccessToken(string AccessTokenValue, string RefreshToken, DateTime ExpiresAt);
public record UserDto(Guid Id, string Username, string Email, string? Phone, string Status);
public record WalletResponse(Guid Id, Guid UserId, long BalanceCents, long BonusBalanceCents);
public record GameDto(Guid Id, string Title, string? IconUrl, bool IsInstalled, List<string> Categories);
public record SessionDto(Guid Id, DateTime StartTime, decimal RatePerMinute, string Status);
public record TariffDto(Guid Id, string Name, decimal PricePerMinute, decimal PricePerHour, bool IsActive);
