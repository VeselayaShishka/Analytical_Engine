using System.Text;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class ImportServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDatasetRepository> _datasetRepoMock = new();
    private readonly Mock<IDatasetVersionRepository> _versionRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IErrorLogRepository> _errorLogRepoMock = new();
    private readonly CSVImportService _csvService;
    private readonly JSONImportService _jsonService;

    public ImportServiceTests()
    {
        _uowMock.Setup(u => u.Datasets).Returns(_datasetRepoMock.Object);
        _uowMock.Setup(u => u.DatasetVersions).Returns(_versionRepoMock.Object);
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.ErrorLogs).Returns(_errorLogRepoMock.Object);

        _csvService = new CSVImportService(_uowMock.Object);
        _jsonService = new JSONImportService(_uowMock.Object);
    }

    [Fact]
    public async Task ImportDataset_NonSeekableStream_ReturnsFailure()
    {
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanSeek).Returns(false);

        var schema = new Schema(new Dictionary<string, SchemaField>());
        var metadata = new Metadata(Guid.NewGuid(), "file.csv", "test@example.com", "csv", DateTime.UtcNow);

        var result = await _csvService.ImportDataset(mockStream.Object, schema, metadata);

        Assert.False(result.IsSuccess);
        Assert.Contains("File stream must support seeking", result.Message);
    }

    [Fact]
    public async Task ImportDataset_FileAlreadyImported_ReturnsFailureWithoutTransaction()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        var dataset = new Dataset { Id = datasetId, UserId = userId };
        var user = new User { Id = userId, Email = "test@example.com", Role = Roles.Analyst, IsActive = true };

        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _userRepoMock.Setup(u => u.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var csvContent = "col1,col2\nval1,val2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        var existingVersion = new DatasetVersion { Id = Guid.NewGuid(), DatasetId = datasetId };
        _versionRepoMock.Setup(v => v.GetByDatasetIdAndHashAsync(datasetId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVersion);

        var schema = new Schema(new Dictionary<string, SchemaField>
        {
            ["col1"] = new("string"),
            ["col2"] = new("string")
        });
        var metadata = new Metadata(datasetId, "file.csv", "test@example.com", "csv", DateTime.UtcNow);

        var result = await _csvService.ImportDataset(stream, schema, metadata);

        Assert.False(result.IsSuccess);
        Assert.Contains("already been imported", result.Message);
        _uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportDataset_CsvDuplicateColumns_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dataset { Id = datasetId, UserId = userId });
        _userRepoMock.Setup(u => u.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "test@example.com", Role = Roles.Analyst, IsActive = true });

        var csvContent = "col1,col1\nval1,val2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        var schema = new Schema(new Dictionary<string, SchemaField>());
        var metadata = new Metadata(datasetId, "file.csv", "test@example.com", "csv", DateTime.UtcNow);

        var result = await _csvService.ImportDataset(stream, schema, metadata);

        Assert.False(result.IsSuccess);
        Assert.Contains("duplicate column", result.Message);
    }

    [Fact]
    public async Task ImportDataset_JsonNotArray_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dataset { Id = datasetId, UserId = userId });
        _userRepoMock.Setup(u => u.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "test@example.com", Role = Roles.Analyst, IsActive = true });

        var jsonContent = """{"key": "value"}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));

        var schema = new Schema(new Dictionary<string, SchemaField>());
        var metadata = new Metadata(datasetId, "file.json", "test@example.com", "json", DateTime.UtcNow);

        var result = await _jsonService.ImportDataset(stream, schema, metadata);

        Assert.False(result.IsSuccess);
        Assert.Contains("JSON root element must be an array", result.Message);
    }

    [Fact]
    public async Task ImportDataset_InvalidJsonSyntax_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var datasetId = Guid.NewGuid();
        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dataset { Id = datasetId, UserId = userId });
        _userRepoMock.Setup(u => u.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "test@example.com", Role = Roles.Analyst, IsActive = true });

        var brokenJson = """[{broken json""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(brokenJson));

        var schema = new Schema(new Dictionary<string, SchemaField>());
        var metadata = new Metadata(datasetId, "file.json", "test@example.com", "json", DateTime.UtcNow);

        var result = await _jsonService.ImportDataset(stream, schema, metadata);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid JSON", result.Message);
    }
}
