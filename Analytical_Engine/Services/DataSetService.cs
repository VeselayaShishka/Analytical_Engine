using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services.Interfaces;

namespace Analytical_Engine.Services;

public class DatasetService : IDatasetService
{
    private const int MaxPageSize = 100;
    private readonly IUnitOfWork _unitOfWork;

    public DatasetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DatasetResponseDto> CreateAsync(
        DatasetCreateDto dto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = dto.Name.Trim();

        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Dataset name cannot be empty or whitespace.");
        }

        if (await _unitOfWork.Datasets.NameExistsAsync(userId, normalizedName, null, cancellationToken))
        {
            throw new ConflictException($"Dataset with name '{normalizedName}' already exists.");
        }

        var now = DateTime.UtcNow;

        var dataset = new Dataset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = normalizedName,
            Description = dto.Description?.Trim(),
            CreatedAt = now,
            ModifiedAt = now
        };

        await _unitOfWork.Datasets.AddAsync(dataset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dataset.ToResponseDto(versionsCount: 0);
    }

    public async Task<PagedResultDto<DatasetResponseDto>> GetListAsync(
        Guid userId,
        bool isAdmin,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        Guid? ownerFilter = isAdmin ? null : userId;

        return await _unitOfWork.Datasets.GetPagedAsync(
            ownerFilter,
            search,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public async Task<DatasetResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var dataset = await _unitOfWork.Datasets.GetDtoByIdAsync(id, cancellationToken);

        if (dataset is null || (!isAdmin && dataset.UserId != userId))
        {
            throw new NotFoundException($"Dataset with id '{id}' was not found.");
        }

        return dataset;
    }

    public async Task<DatasetResponseDto> UpdateAsync(
        Guid id,
        DatasetUpdateDto dto,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var dataset = await GetAccessibleDatasetEntityAsync(id, userId, isAdmin, cancellationToken);

        var normalizedName = dto.Name.Trim();

        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Dataset name cannot be empty or whitespace.");
        }

        if (await _unitOfWork.Datasets.NameExistsAsync(dataset.UserId, normalizedName, id, cancellationToken))
        {
            throw new ConflictException($"Dataset with name '{normalizedName}' already exists.");
        }

        dataset.Name = normalizedName;
        dataset.Description = dto.Description?.Trim();
        dataset.ModifiedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _unitOfWork.Datasets.GetDtoByIdAsync(id, cancellationToken)
            ?? dataset.ToResponseDto();
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var dataset = await GetAccessibleDatasetEntityAsync(id, userId, isAdmin, cancellationToken);

        _unitOfWork.Datasets.Delete(dataset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DatasetVersionResponseDto>> GetVersionsAsync(
        Guid datasetId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatasetAccessAsync(datasetId, userId, isAdmin, cancellationToken);

        var versions = await _unitOfWork.DatasetVersions
            .GetByDatasetIdAsync(datasetId, cancellationToken);

        return versions
            .Select(v => v.ToResponseDto(includeColumns: false))
            .ToList();
    }

    public async Task<IReadOnlyList<DatasetColumnResponseDto>> GetColumnsAsync(
        Guid datasetId,
        Guid versionId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatasetAccessAsync(datasetId, userId, isAdmin, cancellationToken);

        var version = await _unitOfWork.DatasetVersions
            .GetWithColumnsAsync(versionId, cancellationToken);

        if (version is null || version.DatasetId != datasetId)
        {
            throw new NotFoundException(
                $"Version '{versionId}' was not found in dataset '{datasetId}'.");
        }

        return version.Columns
            .OrderBy(c => c.Position)
            .Select(c => c.ToResponseDto())
            .ToList();
    }

    public async Task<PagedResultDto<DataRecordResponseDto>> GetRecordsAsync(
        Guid datasetId,
        Guid versionId,
        Guid userId,
        bool isAdmin,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        await EnsureDatasetAccessAsync(datasetId, userId, isAdmin, cancellationToken);

        var version = await _unitOfWork.DatasetVersions
            .GetByIdAsync(versionId, cancellationToken);

        if (version is null || version.DatasetId != datasetId)
        {
            throw new NotFoundException(
                $"Version '{versionId}' was not found in dataset '{datasetId}'.");
        }

        var records = await _unitOfWork.DataRecords
            .GetPagedByVersionIdAsync(versionId, pageNumber, pageSize, cancellationToken);

        var items = records
            .Select(r => r.ToResponseDto())
            .ToList();

        return new PagedResultDto<DataRecordResponseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = version.RecordCount
        };
    }

    private async Task<Dataset> GetAccessibleDatasetEntityAsync(
        Guid datasetId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var dataset = await _unitOfWork.Datasets.GetByIdAsync(datasetId, cancellationToken);

        if (dataset is null || (!isAdmin && dataset.UserId != userId))
        {
            throw new NotFoundException($"Dataset with id '{datasetId}' was not found.");
        }

        return dataset;
    }

    private async Task EnsureDatasetAccessAsync(
        Guid datasetId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var dataset = await _unitOfWork.Datasets.GetDtoByIdAsync(datasetId, cancellationToken);

        if (dataset is null || (!isAdmin && dataset.UserId != userId))
        {
            throw new NotFoundException($"Dataset with id '{datasetId}' was not found.");
        }
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "PageNumber must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"PageSize must be between 1 and {MaxPageSize}.");
        }
    }
}