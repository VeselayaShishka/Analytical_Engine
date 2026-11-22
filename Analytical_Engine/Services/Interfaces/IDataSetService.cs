using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;

namespace Analytical_Engine.Services.Interfaces;

public interface IDatasetService
{
    Task<DatasetResponseDto> CreateAsync(
        DatasetCreateDto dto,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<DatasetResponseDto>> GetListAsync(
        Guid userId,
        bool isAdmin,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<DatasetResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<DatasetResponseDto> UpdateAsync(
        Guid id,
        DatasetUpdateDto dto,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatasetVersionResponseDto>> GetVersionsAsync(
        Guid datasetId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatasetColumnResponseDto>> GetColumnsAsync(
        Guid datasetId,
        Guid versionId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<DataRecordResponseDto>> GetRecordsAsync(
        Guid datasetId,
        Guid versionId,
        Guid userId,
        bool isAdmin,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}