using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class ReportRepository : Repository<Report>, IReportRepository
{
    public ReportRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Report>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Report>> GetByVersionIdAsync(Guid datasetVersionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(r => r.DatasetVersionId == datasetVersionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<PagedResultDto<ReportListItemDto>> GetPagedAsync(
        Guid? userId,
        Guid? datasetVersionId,
        string? reportType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Report> query = DbSet.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (datasetVersionId.HasValue)
        {
            query = query.Where(r => r.DatasetVersionId == datasetVersionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(reportType))
        {
            query = query.Where(r => r.ReportType == reportType);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportListItemDto
            {
                Id = r.Id,
                UserId = r.UserId,
                DatasetVersionId = r.DatasetVersionId,
                Name = r.Name,
                ReportType = r.ReportType,
                CreatedAt = r.CreatedAt,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ReportListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}