using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arceus.Infrastructure.Persistence.Repositories;

public class ContractorRepository : IContractorRepository
{
    private readonly ApplicationDbContext _context;

    public ContractorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Contractor?> GetByIdAsync(long contractorId, CancellationToken cancellationToken = default)
    {
        return await _context.Contractors
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Id == contractorId, cancellationToken);
    }

    public async Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default)
    {
        await _context.Contractors.AddAsync(contractor, cancellationToken);
    }

    public void Update(Contractor contractor)
    {
        _context.Contractors.Update(contractor);
    }
}