namespace Sapphire.Client.Desktop.Models;

public record AuthResponse(TokenDto Tokens, UserDto User);
public record TokenDto(string AccessToken, string RefreshToken,
    DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);
public record UserDto(Guid Id, string Username, string Email, string? Phone, string Status,
    bool MustChangePassword);
public record WalletResponse(Guid Id, Guid UserId, long BalanceCents, long BonusBalanceCents);
public record GameDto(Guid Id, string Title, string? IconUrl, bool IsInstalled, List<string> Categories);
public record SessionDto(Guid Id, DateTime StartTime, decimal RatePerMinute, string Status);
public record TariffDto(Guid Id, string Name, decimal PricePerMinute, decimal PricePerHour, bool IsActive);
