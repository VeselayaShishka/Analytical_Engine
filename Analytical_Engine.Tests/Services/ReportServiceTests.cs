using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Interfaces;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDatasetRepository> _datasetRepoMock = new();
    private readonly Mock<IReportRepository> _reportRepoMock = new();
    private readonly Mock<IDatasetVersionRepository> _versionRepoMock = new();
    private readonly Mock<IAnalysisService> _analysisMock = new();
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _uowMock.Setup(u => u.Datasets).Returns(_datasetRepoMock.Object);
        _uowMock.Setup(u => u.Reports).Returns(_reportRepoMock.Object);
        _uowMock.Setup(u => u.DatasetVersions).Returns(_versionRepoMock.Object);
        _reportService = new ReportService(_uowMock.Object, _analysisMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidQueryReport_CreatesAndReturnsReport()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var version = new DatasetVersion
        {
            Id = versionId,
            DatasetId = datasetId,
            Columns = [new DatasetColumn { Name = "col1", DataType = "string" }]
        };

        _versionRepoMock.Setup(v => v.GetWithColumnsAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatasetResponseDto { Id = datasetId, UserId = userId });

        var queryRequest = new AnalysisQueryRequestDto
        {
            DatasetVersionId = versionId,
            Aggregates = [new AggregateRequestDto { Function = AggregateFunction.Count }]
        };

        var dto = new ReportCreateDto
        {
            DatasetVersionId = versionId,
            Name = "Query Report",
            ReportType = ReportType.Aggregation,
            QueryRequest = queryRequest
        };

        _analysisMock.Setup(a => a.ExecuteAsync(queryRequest, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnalysisResultDto<IReadOnlyList<GroupResultItem>>
            {
                DatasetVersionId = versionId,
                Operation = "count",
                RecordsProcessed = 10,
                Result = []
            });

        var result = await _reportService.CreateAsync(dto, userId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal("Query Report", result.Name);
        Assert.Equal(ReportType.Aggregation.ToString(), result.ReportType);
        _reportRepoMock.Verify(r => r.AddAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException(string name)
    {
        var dto = new ReportCreateDto { Name = name, DatasetVersionId = Guid.NewGuid() };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _reportService.CreateAsync(dto, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task CreateAsync_NeitherQueryNorStatsProvided_ThrowsRequestValidationException()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var version = new DatasetVersion
        {
            Id = versionId,
            DatasetId = datasetId,
            Columns = []
        };

        _versionRepoMock.Setup(v => v.GetWithColumnsAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatasetResponseDto { Id = datasetId, UserId = userId });

        var dto = new ReportCreateDto
        {
            Name = "Empty Report",
            DatasetVersionId = versionId,
            QueryRequest = null,
            StatisticsRequest = null
        };

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _reportService.CreateAsync(dto, userId, isAdmin: false));
    }

    [Fact]
    public async Task GetByIdAsync_Owner_ReturnsReport()
    {
        var reportId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var report = new Report
        {
            Id = reportId,
            UserId = userId,
            Name = "My Report",
            ReportType = ReportType.Aggregation.ToString(),
            ResultData = """{"result":[]}""",
            DatasetVersionId = versionId,
            DatasetVersion = new DatasetVersion
            {
                Id = versionId,
                Dataset = new Dataset { Name = "DS" }
            }
        };

        _reportRepoMock.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _reportService.GetByIdAsync(reportId, userId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(reportId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotOwnerAndNotAdmin_ThrowsNotFoundException()
    {
        var reportId = Guid.NewGuid();
        var report = new Report
        {
            Id = reportId,
            UserId = Guid.NewGuid(),
            Name = "Other User Report"
        };

        _reportRepoMock.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _reportService.GetByIdAsync(reportId, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task DeleteAsync_Owner_DeletesReport()
    {
        var reportId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var report = new Report { Id = reportId, UserId = userId, Name = "Delete Report" };

        _reportRepoMock.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        await _reportService.DeleteAsync(reportId, userId, isAdmin: false);

        _reportRepoMock.Verify(r => r.Delete(report), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
