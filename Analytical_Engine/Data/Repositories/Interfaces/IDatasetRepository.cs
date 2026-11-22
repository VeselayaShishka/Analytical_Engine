using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDatasetRepository : IRepository<Dataset>
{
    Task<IReadOnlyList<Dataset>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Dataset?> GetByNameAndUserIdAsync(string name, Guid userId, CancellationToken cancellationToken = default);
    Task<Dataset?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<DatasetResponseDto>> GetPagedAsync(
        Guid? userId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DatasetResponseDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeDatasetId = null,
        CancellationToken cancellationToken = default);
}