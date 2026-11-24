using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Services;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDatasetRepository> _datasetRepoMock = new();
    private readonly Mock<IReportRepository> _reportRepoMock = new();
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _uowMock.Setup(u => u.Datasets).Returns(_datasetRepoMock.Object);
        _uowMock.Setup(u => u.Reports).Returns(_reportRepoMock.Object);
        _dashboardService = new DashboardService(_uowMock.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_AnalystUser_FiltersByUserId()
    {
        var userId = Guid.NewGuid();
        bool isAdmin = false;

        _datasetRepoMock.Setup(d => d.GetSummaryMetricsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatasetsCount: 5, VersionsCount: 12, RecordsCount: 1000));

        var pagedDatasets = new PagedResultDto<DatasetResponseDto>
        {
            Items = [new DatasetResponseDto { Id = Guid.NewGuid(), Name = "D1" }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 5
        };
        _datasetRepoMock.Setup(d => d.GetPagedAsync(userId, null, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDatasets);

        var pagedReports = new PagedResultDto<ReportListItemDto>
        {
            Items = [new ReportListItemDto { Id = Guid.NewGuid(), Name = "R1" }],
            TotalCount = 10,
            PageNumber = 1,
            PageSize = 5
        };
        _reportRepoMock.Setup(r => r.GetPagedAsync(userId, null, null, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedReports);

        var summary = await _dashboardService.GetSummaryAsync(userId, isAdmin);

        Assert.NotNull(summary);
        Assert.Equal(5, summary.TotalDatasets);
        Assert.Equal(12, summary.TotalVersions);
        Assert.Equal(1000, summary.TotalRecords);
        Assert.Equal(10, summary.TotalReports);
        Assert.Single(summary.RecentDatasets);
        Assert.Single(summary.RecentReports);
    }

    [Fact]
    public async Task GetSummaryAsync_AdminUser_PassesNullOwnerFilter()
    {
        var adminId = Guid.NewGuid();
        bool isAdmin = true;

        _datasetRepoMock.Setup(d => d.GetSummaryMetricsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatasetsCount: 50, VersionsCount: 100, RecordsCount: 500000));

        var pagedDatasets = new PagedResultDto<DatasetResponseDto>
        {
            Items = [],
            TotalCount = 50,
            PageNumber = 1,
            PageSize = 5
        };
        _datasetRepoMock.Setup(d => d.GetPagedAsync(null, null, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDatasets);

        var pagedReports = new PagedResultDto<ReportListItemDto>
        {
            Items = [],
            TotalCount = 25,
            PageNumber = 1,
            PageSize = 5
        };
        _reportRepoMock.Setup(r => r.GetPagedAsync(null, null, null, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedReports);

        var summary = await _dashboardService.GetSummaryAsync(adminId, isAdmin);

        Assert.NotNull(summary);
        Assert.Equal(50, summary.TotalDatasets);
        Assert.Equal(100, summary.TotalVersions);
        Assert.Equal(500000, summary.TotalRecords);
        Assert.Equal(25, summary.TotalReports);
    }
}
