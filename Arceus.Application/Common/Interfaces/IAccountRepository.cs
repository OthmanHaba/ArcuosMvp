using Arceus.Domain.Entities;
using Arceus.Domain.Enums;

namespace Arceus.Application.Common.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(long accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetByOwnerAndTypeAsync(long ownerId, AccountType accountType, CancellationToken cancellationToken = default);
    Task<List<Account>> GetByOwnerIdAsync(long ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    void Update(Account account);
}