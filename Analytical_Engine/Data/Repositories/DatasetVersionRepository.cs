using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DatasetVersionRepository : Repository<DatasetVersion>, IDatasetVersionRepository
{
    public DatasetVersionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DatasetVersion>> GetByDatasetIdAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(v => v.DatasetId == datasetId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<DatasetVersion?> GetLatestVersionAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.DatasetId == datasetId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DatasetVersion?> GetWithColumnsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(v => v.Columns)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
    }

    public async Task<DatasetVersion?> GetByHashAsync(String hash, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(v => v.FileHash.Equals(hash, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

}