using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.DTOs.Dashboard;
using Analytical_Engine.Services.Interfaces;

namespace Analytical_Engine.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        Guid? ownerFilter = isAdmin ? null : userId;

        var (datasetsCount, versionsCount, recordsCount) =
            await _unitOfWork.Datasets.GetSummaryMetricsAsync(ownerFilter, cancellationToken);

        var recentDatasets = await _unitOfWork.Datasets.GetPagedAsync(
            ownerFilter,
            search: null,
            pageNumber: 1,
            pageSize: 5,
            cancellationToken);

        var recentReports = await _unitOfWork.Reports.GetPagedAsync(
            ownerFilter,
            datasetVersionId: null,
            reportType: null,
            pageNumber: 1,
            pageSize: 5,
            cancellationToken);

        return new DashboardSummaryDto
        {
            TotalDatasets = datasetsCount,
            TotalVersions = versionsCount,
            TotalRecords = recordsCount,
            TotalReports = recentReports.TotalCount,
            RecentDatasets = recentDatasets.Items,
            RecentReports = recentReports.Items
        };
    }
}