using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Infrastructure.Persistence.Repositories;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AuthDbContext _context;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IRefreshTokenRepository? _refreshTokens;
    private IActivityHistoryRepository? _activityHistory;
    private IOutboxRepository? _outbox;

    public UnitOfWork(AuthDbContext context) => _context = context;

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IActivityHistoryRepository ActivityHistory => _activityHistory ??= new ActivityHistoryRepository(_context);
    public IOutboxRepository Outbox => _outbox ??= new OutboxRepository(_context);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.RollbackTransactionAsync(ct);
    }

    public void Dispose() => _context.Dispose();
}
