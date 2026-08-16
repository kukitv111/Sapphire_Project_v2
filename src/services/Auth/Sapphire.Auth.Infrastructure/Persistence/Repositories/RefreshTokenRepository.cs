using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Repositories;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context) => _context = context;

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.RefreshTokens.FindAsync([id], ct);

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await _context.RefreshTokens.AddAsync(refreshToken, ct);

    public void Update(RefreshToken refreshToken) => _context.RefreshTokens.Update(refreshToken);

    public async Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }
    }

    public async Task<int> CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        var expired = await _context.RefreshTokens
            .Where(t => t.ExpiresAt <= DateTime.UtcNow || t.RevokedAt != null)
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(expired);
        return expired.Count;
    }
}
