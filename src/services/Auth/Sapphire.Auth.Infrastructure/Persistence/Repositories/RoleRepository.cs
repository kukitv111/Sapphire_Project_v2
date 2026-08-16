using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Infrastructure.Persistence;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context) => _context = context;

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Roles.ToListAsync(ct);

    public async Task<IReadOnlyList<Role>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Roles.Where(r => r.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await _context.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _context.Roles.AddAsync(role, ct);

    public void Update(Role role) => _context.Roles.Update(role);

    public void Delete(Role role) => _context.Roles.Remove(role);

    public async Task<bool> IsNameTakenAsync(string name, CancellationToken ct = default) =>
        await _context.Roles.AnyAsync(r => r.NormalizedName == name.ToUpperInvariant(), ct);
}
