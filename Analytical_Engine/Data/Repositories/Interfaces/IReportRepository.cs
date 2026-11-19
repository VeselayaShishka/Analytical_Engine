using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IReportRepository : IRepository<Report>
{
    Task<IReadOnlyList<Report>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Report>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default);
}