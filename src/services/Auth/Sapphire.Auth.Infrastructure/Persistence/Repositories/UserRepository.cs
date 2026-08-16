using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.Username.Value == normalized || u.Email.Value == normalized,
                cancellationToken);
    }

    public Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }

    public Task<bool> IsEmailRegisteredAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> IsPhoneRegisteredAsync(PhoneNumber phone, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Phone == phone, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public void Delete(User user)
    {
        _dbContext.Users.Remove(user);
    }

    public async Task<IReadOnlyList<User>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(u => u.BranchId == branchId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(u => u.Status == Domain.Enums.UserStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(u => u.Roles.Any(r => r.RoleId == roleId))
            .ToListAsync(cancellationToken);
    }
}
