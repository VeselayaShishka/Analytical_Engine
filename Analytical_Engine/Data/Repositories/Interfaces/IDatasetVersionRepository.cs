using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDatasetVersionRepository : IRepository<DatasetVersion>
{
    Task<IReadOnlyList<DatasetVersion>> GetByDatasetIdAsync(Guid datasetId, CancellationToken cancellationToken = default);
    Task<DatasetVersion?> GetLatestVersionAsync(Guid datasetId, CancellationToken cancellationToken = default);
    Task<DatasetVersion?> GetWithColumnsAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task<DatasetVersion?> GetByHashAsync(String hash, CancellationToken cancellationToken = default);
}