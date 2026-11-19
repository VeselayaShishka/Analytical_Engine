using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DatasetColumnRepository : Repository<DatasetColumn>, IDatasetColumnRepository
{
    public DatasetColumnRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DatasetColumn>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.DatasetVersionId == datasetVersionId)
            .OrderBy(c => c.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DatasetColumn> columns, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(columns, cancellationToken);
    }
}