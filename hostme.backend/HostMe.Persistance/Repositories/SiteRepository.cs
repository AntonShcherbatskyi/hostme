using HostMe.Domain.Entities;
using HostMe.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HostMe.Persistance.Repositories;

public class SiteRepository : ISiteRepository
{
    private readonly HostMeDbContext _dbContext;

    public SiteRepository(HostMeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sites.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Site>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sites
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        await _dbContext.Sites.AddAsync(site, cancellationToken);
    }

    public void Delete(Site site)
    {
        _dbContext.Sites.Remove(site);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
