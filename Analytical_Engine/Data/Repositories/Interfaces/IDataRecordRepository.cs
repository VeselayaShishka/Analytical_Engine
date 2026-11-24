using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDataRecordRepository : IRepository<DataRecord>
{
    Task<IReadOnlyList<DataRecord>> GetPagedByVersionIdAsync(Guid datasetVersionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamDataByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default);
}