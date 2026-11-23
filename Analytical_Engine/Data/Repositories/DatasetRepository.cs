using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DatasetRepository : Repository<Dataset>, IDatasetRepository
{
    public DatasetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Dataset>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dataset?> GetByNameAndUserIdAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await DbSet
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Name == normalizedName, cancellationToken);
    }

    public async Task<Dataset?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<PagedResultDto<DatasetResponseDto>> GetPagedAsync(
        Guid? userId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Dataset> query = DbSet.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.ModifiedAt)
            .ThenBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DatasetResponseDto
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.Name,
                Description = d.Description,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt,
                VersionsCount = d.Versions.Count()
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DatasetResponseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DatasetResponseDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DatasetResponseDto
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.Name,
                Description = d.Description,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt,
                VersionsCount = d.Versions.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeDatasetId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        var query = DbSet.Where(d => d.UserId == userId && d.Name == normalizedName);

        if (excludeDatasetId.HasValue)
        {
            query = query.Where(d => d.Id != excludeDatasetId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
    
    public async Task<(int DatasetsCount, int VersionsCount, long RecordsCount)> GetSummaryMetricsAsync(
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Dataset> query = DbSet.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId.Value);
        }

        var metrics = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DatasetsCount = g.Count(),
                VersionsCount = g.Sum(d => d.Versions.Count()),
                RecordsCount = g.SelectMany(d => d.Versions).Sum(v => v.RecordCount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return metrics is null
            ? (0, 0, 0L)
            : (metrics.DatasetsCount, metrics.VersionsCount, metrics.RecordsCount);
    }
}