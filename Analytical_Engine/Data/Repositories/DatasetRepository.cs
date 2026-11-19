using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DatasetRepository : Repository<Dataset>, IDatasetRepository
{
    public DatasetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Dataset>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dataset?> GetByNameAndUserIdAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Name == name, cancellationToken);
    }

    public async Task<Dataset?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
}