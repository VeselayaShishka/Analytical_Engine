using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDatasetColumnRepository : IRepository<DatasetColumn>
{
    Task<IReadOnlyList<DatasetColumn>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DatasetColumn> columns, CancellationToken cancellationToken = default);
}