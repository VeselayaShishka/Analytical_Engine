using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDatasetRepository : IRepository<Dataset>
{
    Task<IReadOnlyList<Dataset>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Dataset?> GetByNameAndUserIdAsync(string name, Guid userId, CancellationToken cancellationToken = default);
    Task<Dataset?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken = default);
}