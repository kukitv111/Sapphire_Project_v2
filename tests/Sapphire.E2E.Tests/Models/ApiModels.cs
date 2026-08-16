namespace Sapphire.E2E.Tests.Models;

public record RegisterRequest(string Username, string Email, string Password, string? Phone = null, Guid? BranchId = null);
public record LoginRequest(string Login, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(Guid Id, string Username, string Email, string? Phone, string Status, bool IsBanned, List<RoleDto> Roles);
public record RoleDto(Guid Id, string Name, string? Description, bool IsSystem);

public record TopUpRequest(long AmountCents, string Method);
public record PaymentResponse(Guid Id, Guid UserId, long AmountCents, string Method, string Status, string Type, DateTime CreatedAt);
public record WalletResponse(Guid Id, Guid UserId, long BalanceCents, long BonusBalanceCents);

public record StartSessionRequest(Guid ComputerId, Guid TariffId);
public record SessionResponse(Guid Id, Guid ComputerId, Guid UserId, Guid TariffId, DateTime StartTime, DateTime? EndTime, string Status, decimal TotalCost);

public record TariffDto(Guid Id, string Name, string Type, decimal PricePerMinute, decimal PricePerHour, bool IsActive);
