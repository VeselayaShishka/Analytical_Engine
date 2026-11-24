using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IReportRepository : IRepository<Report>
{
    Task<PagedResultDto<ReportListItemDto>> GetPagedAsync(
        Guid? userId,
        Guid? datasetVersionId,
        string? reportType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}