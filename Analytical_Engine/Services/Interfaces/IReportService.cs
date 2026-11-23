using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;

namespace Analytical_Engine.Services.Interfaces;

public interface IReportService
{
    Task<ReportResponseDto> CreateAsync(
        ReportCreateDto dto,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<ReportListItemDto>> GetListAsync(
        Guid userId,
        bool isAdmin,
        Guid? datasetVersionId = null,
        ReportType? reportType = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ReportResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}