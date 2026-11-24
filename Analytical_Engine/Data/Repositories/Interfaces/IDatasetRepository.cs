using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IDatasetRepository : IRepository<Dataset>
{
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
    
    Task<(int DatasetsCount, int VersionsCount, long RecordsCount)> GetSummaryMetricsAsync(
        Guid? userId,
        CancellationToken cancellationToken = default);
}