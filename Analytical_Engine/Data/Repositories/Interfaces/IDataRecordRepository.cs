using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDataRecordRepository : IRepository<DataRecord>
{
    Task<IReadOnlyList<DataRecord>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataRecord>> GetPagedByVersionIdAsync(Guid datasetVersionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<long> GetCountByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DataRecord> records, CancellationToken cancellationToken = default);
}