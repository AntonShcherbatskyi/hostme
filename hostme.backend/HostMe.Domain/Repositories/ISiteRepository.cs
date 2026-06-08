using HostMe.Domain.Entities;

namespace HostMe.Domain.Repositories;

public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Site>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Site site, CancellationToken cancellationToken = default);
    void Delete(Site site);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
