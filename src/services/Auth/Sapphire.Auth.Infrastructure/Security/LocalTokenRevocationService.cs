using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Enums;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.Auth.Infrastructure.Security;

public sealed class LocalTokenRevocationService(AuthDbContext db) : ITokenRevocationService
{
    public async Task<TokenVersionState?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.TokenVersion, u.Status, u.MustChangePassword })
            .SingleOrDefaultAsync(ct);
        return user is null ? null : new TokenVersionState(user.TokenVersion,
            user.Status == UserStatus.Active, user.MustChangePassword);
    }
}
