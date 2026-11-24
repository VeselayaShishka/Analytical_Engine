using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Analysis;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class AnalysisServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDatasetRepository> _datasetRepoMock = new();
    private readonly Mock<IDatasetVersionRepository> _versionRepoMock = new();
    private readonly Mock<IDataRecordRepository> _recordRepoMock = new();
    private readonly ColumnValueConverter _converter = new();
    private readonly AnalysisRequestValidator _validator;
    private readonly AnalysisEngine _engine;
    private readonly AnalysisService _analysisService;

    public AnalysisServiceTests()
    {
        _uowMock.Setup(u => u.Datasets).Returns(_datasetRepoMock.Object);
        _uowMock.Setup(u => u.DatasetVersions).Returns(_versionRepoMock.Object);
        _uowMock.Setup(u => u.DataRecords).Returns(_recordRepoMock.Object);

        _validator = new AnalysisRequestValidator(_converter);
        _engine = new AnalysisEngine(_converter);
        _analysisService = new AnalysisService(_uowMock.Object, _validator, _engine);
    }

    private static async IAsyncEnumerable<string> ToAsyncStream(IEnumerable<string> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
            await Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteAsync_ValidQuery_CalculatesAndReturnsResult()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var version = new DatasetVersion
        {
            Id = versionId,
            DatasetId = datasetId,
            Columns =
            [
                new DatasetColumn { Name = "country", DataType = "string", Position = 0 },
                new DatasetColumn { Name = "score", DataType = "decimal", Position = 1 }
            ]
        };

        _versionRepoMock.Setup(v => v.GetWithColumnsAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatasetResponseDto { Id = datasetId, UserId = userId });

        var rows = new[]
        {
            """{"country":"US","score":"100.5"}""",
            """{"country":"US","score":"99.5"}""",
            """{"country":"UK","score":"80"}"""
        };

        _recordRepoMock.Setup(r => r.StreamDataByVersionIdAsync(versionId, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncStream(rows));

        var request = new AnalysisQueryRequestDto
        {
            DatasetVersionId = versionId,
            GroupBy = "country",
            Aggregates =
            [
                new AggregateRequestDto { Function = AggregateFunction.Sum, Column = "score", Alias = "total_score" }
            ]
        };

        var result = await _analysisService.ExecuteAsync(request, userId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(versionId, result.DatasetVersionId);
        Assert.Equal(3, result.RecordsProcessed);
        Assert.Equal(2, result.Result.Count);

        var usGroup = result.Result.First(g => g.Key == "US");
        Assert.Equal(2, usGroup.Count);
        Assert.Equal(200.0m, usGroup.Values["total_score"]);

        var ukGroup = result.Result.First(g => g.Key == "UK");
        Assert.Equal(1, ukGroup.Count);
        Assert.Equal(80m, ukGroup.Values["total_score"]);
    }

    [Fact]
    public async Task GetStatisticsAsync_ValidNumericColumn_CalculatesWelfordStatistics()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var version = new DatasetVersion
        {
            Id = versionId,
            DatasetId = datasetId,
            Columns =
            [
                new DatasetColumn { Name = "score", DataType = "decimal", Position = 0 }
            ]
        };

        _versionRepoMock.Setup(v => v.GetWithColumnsAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatasetResponseDto { Id = datasetId, UserId = userId });

        var rows = new[]
        {
            """{"score":"10"}""",
            """{"score":"20"}""",
            """{"score":"30"}"""
        };

        _recordRepoMock.Setup(r => r.StreamDataByVersionIdAsync(versionId, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncStream(rows));

        var request = new StatisticsRequestDto
        {
            DatasetVersionId = versionId,
            Column = "score"
        };

        var result = await _analysisService.GetStatisticsAsync(request, userId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(3, result.RecordsProcessed);
        Assert.Single(result.Result);

        var stats = result.Result[0];
        Assert.Equal(3, stats.Count);
        Assert.Equal(10m, stats.Min);
        Assert.Equal(30m, stats.Max);
        Assert.Equal(20.0, stats.Mean);
    }

    [Fact]
    public async Task ExecuteAsync_UnauthorizedVersionAccess_ThrowsNotFoundException()
    {
        var versionId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var version = new DatasetVersion
        {
            Id = versionId,
            DatasetId = datasetId,
            Columns = []
        };

        _versionRepoMock.Setup(v => v.GetWithColumnsAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatasetResponseDto { Id = datasetId, UserId = ownerId });

        var request = new AnalysisQueryRequestDto { DatasetVersionId = versionId };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _analysisService.ExecuteAsync(request, otherUserId, isAdmin: false));
    }
}
