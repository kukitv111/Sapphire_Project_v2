using Microsoft.EntityFrameworkCore;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Infrastructure.Persistence.Repositories;

public sealed class ComputerRepository : IComputerRepository
{
    private readonly SessionDbContext _context;

    public ComputerRepository(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Computer>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var computer = await _context.Computers
            .FindAsync(new object[] { id }, cancellationToken);

        return computer != null
            ? Result.Success(computer)
            : Result.Failure<Computer>(Error.Create("COMPUTER_NOT_FOUND", "Computer not found"));
    }

    public async Task AddAsync(Computer computer, CancellationToken cancellationToken)
    {
        await _context.Computers.AddAsync(computer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Implement other methods as needed
}
