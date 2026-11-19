using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DataRecordRepository : Repository<DataRecord>, IDataRecordRepository
{
    public DataRecordRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DataRecord>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(r => r.DatasetVersionId == datasetVersionId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataRecord>> GetPagedByVersionIdAsync(Guid datasetVersionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(r => r.DatasetVersionId == datasetVersionId)
            .OrderBy(r => r.RowNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetCountByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.DatasetVersionId == datasetVersionId)
            .LongCountAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(records, cancellationToken);
    }
}