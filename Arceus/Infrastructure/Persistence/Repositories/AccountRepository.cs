using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arceus.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(long accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Owner)
            .FirstOrDefaultAsync(a => a.Idddd == accountId, cancellationToken);
    }

    public async Task<Account?> GetByOwnerAndTypeAsync(long ownerId, AccountType accountType, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Owner)
            .FirstOrDefaultAsync(a => a.OwnerId == ownerId && a.AccountType == accountType, cancellationToken);
    }

    public async Task<List<Account>> GetByOwnerIdAsync(long ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Owner)
            .Where(a => a.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
    }

    public void Update(Account account)
    {
        _context.Accounts.Update(account);
    }
}