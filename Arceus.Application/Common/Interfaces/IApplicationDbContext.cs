using Arceus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arceus.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Contractor> Contractors { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<JournalEntry> JournalEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}