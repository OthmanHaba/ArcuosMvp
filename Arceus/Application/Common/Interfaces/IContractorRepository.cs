using Arceus.Domain.Entities;

namespace Arceus.Application.Common.Interfaces;

public interface IContractorRepository
{
    Task<Contractor?> GetByIdAsync(long contractorId, CancellationToken cancellationToken = default);
    Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default);
    void Update(Contractor contractor);
}