using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.DTOs.Reports;

namespace Analytical_Engine.Models.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalDatasets { get; set; }
    public int TotalVersions { get; set; }
    public long TotalRecords { get; set; }
    public long TotalReports { get; set; }
    public IReadOnlyList<DatasetResponseDto> RecentDatasets { get; set; } = Array.Empty<DatasetResponseDto>();
    public IReadOnlyList<ReportListItemDto> RecentReports { get; set; } = Array.Empty<ReportListItemDto>();
}