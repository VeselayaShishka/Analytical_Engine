using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class DatasetServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDatasetRepository> _datasetRepoMock = new();
    private readonly Mock<IDatasetVersionRepository> _versionRepoMock = new();
    private readonly Mock<IDataRecordRepository> _recordRepoMock = new();
    private readonly Mock<IDatasetColumnRepository> _columnRepoMock = new();
    private readonly DatasetService _datasetService;

    public DatasetServiceTests()
    {
        _uowMock.Setup(u => u.Datasets).Returns(_datasetRepoMock.Object);
        _uowMock.Setup(u => u.DatasetVersions).Returns(_versionRepoMock.Object);
        _uowMock.Setup(u => u.DataRecords).Returns(_recordRepoMock.Object);
        _uowMock.Setup(u => u.DatasetColumns).Returns(_columnRepoMock.Object);
        _datasetService = new DatasetService(_uowMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsDataset()
    {
        var userId = Guid.NewGuid();
        var dto = new DatasetCreateDto { Name = "Sales Data", Description = "Quarterly sales" };

        _datasetRepoMock.Setup(d => d.NameExistsAsync(userId, "Sales Data", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _datasetService.CreateAsync(dto, userId);

        Assert.NotNull(result);
        Assert.Equal("Sales Data", result.Name);
        Assert.Equal("Quarterly sales", result.Description);
        Assert.Equal(userId, result.UserId);
        _datasetRepoMock.Verify(d => d.AddAsync(It.IsAny<Dataset>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException(string name)
    {
        var dto = new DatasetCreateDto { Name = name };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _datasetService.CreateAsync(dto, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var dto = new DatasetCreateDto { Name = "Existing Name" };

        _datasetRepoMock.Setup(d => d.NameExistsAsync(userId, "Existing Name", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _datasetService.CreateAsync(dto, userId));
    }

    [Fact]
    public async Task GetByIdAsync_OwnerUser_ReturnsDatasetDto()
    {
        var datasetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new DatasetResponseDto { Id = datasetId, UserId = userId, Name = "My Dataset" };

        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _datasetService.GetByIdAsync(datasetId, userId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(datasetId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AdminUser_ReturnsDatasetDtoEvenIfNotOwner()
    {
        var datasetId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var dto = new DatasetResponseDto { Id = datasetId, UserId = ownerId, Name = "Other Dataset" };

        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _datasetService.GetByIdAsync(datasetId, adminId, isAdmin: true);

        Assert.NotNull(result);
        Assert.Equal(datasetId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_OtherUserNotAdmin_ThrowsNotFoundException()
    {
        var datasetId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var dto = new DatasetResponseDto { Id = datasetId, UserId = ownerId, Name = "Secret Dataset" };

        _datasetRepoMock.Setup(d => d.GetDtoByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _datasetService.GetByIdAsync(datasetId, otherUserId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesDataset()
    {
        var datasetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entity = new Dataset { Id = datasetId, UserId = userId, Name = "Old Name" };
        var dto = new DatasetUpdateDto { Name = "New Name", Description = "Updated desc" };

        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _datasetRepoMock.Setup(d => d.NameExistsAsync(userId, "New Name", datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _versionRepoMock.Setup(v => v.GetByDatasetIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DatasetVersion>());

        var result = await _datasetService.UpdateAsync(datasetId, dto, userId, isAdmin: false);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("Updated desc", result.Description);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_OwnerOrAdmin_DeletesDataset()
    {
        var datasetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entity = new Dataset { Id = datasetId, UserId = userId, Name = "Delete Me" };

        _datasetRepoMock.Setup(d => d.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        await _datasetService.DeleteAsync(datasetId, userId, isAdmin: false);

        _datasetRepoMock.Verify(d => d.Delete(entity), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
