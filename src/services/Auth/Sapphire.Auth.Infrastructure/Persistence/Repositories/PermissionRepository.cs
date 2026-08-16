using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Repositories;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly AuthDbContext _context;

    public PermissionRepository(AuthDbContext context) => _context = context;

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Permissions.FindAsync([id], ct);

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await _context.Permissions.FirstOrDefaultAsync(p => p.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Permissions.ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Permissions.Where(p => p.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByCategoryAsync(string category, CancellationToken ct = default) =>
        await _context.Permissions.Where(p => p.Category == category).ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await _context.Permissions.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public async Task AddAsync(Permission permission, CancellationToken ct = default) =>
        await _context.Permissions.AddAsync(permission, ct);

    public void Update(Permission permission) => _context.Permissions.Update(permission);

    public void Delete(Permission permission) => _context.Permissions.Remove(permission);

    public async Task<bool> IsCodeTakenAsync(string code, CancellationToken ct = default) =>
        await _context.Permissions.AnyAsync(p => p.Code == code.ToUpperInvariant(), ct);
}
