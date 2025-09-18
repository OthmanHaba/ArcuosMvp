using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arceus.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(long transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.JournalEntries)
            .ThenInclude(je => je.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public void Update(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
    }
}