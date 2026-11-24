using System.Security.Claims;
using Analytical_Engine.Controllers;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Controllers;

public class DatasetsControllerTests
{
    private readonly Mock<IDatasetService> _datasetServiceMock = new();
    private readonly DatasetsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public DatasetsControllerTests()
    {
        _controller = new DatasetsController(_datasetServiceMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, "testanalyst"),
            new Claim(ClaimTypes.Role, Roles.Analyst)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtActionResult()
    {
        var dto = new DatasetCreateDto { Name = "New Dataset" };
        var created = new DatasetResponseDto { Id = Guid.NewGuid(), Name = "New Dataset", UserId = _userId };

        _datasetServiceMock.Setup(s => s.CreateAsync(dto, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var actionResult = await _controller.Create(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(DatasetsController.GetById), createdResult.ActionName);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task GetList_ReturnsPagedResult()
    {
        var paged = new PagedResultDto<DatasetResponseDto>
        {
            Items = [new DatasetResponseDto { Id = Guid.NewGuid(), Name = "D1" }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };

        _datasetServiceMock.Setup(s => s.GetListAsync(_userId, false, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var actionResult = await _controller.GetList(null, 1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(paged, okResult.Value);
    }

    [Fact]
    public async Task GetById_ExistingDataset_ReturnsOkResult()
    {
        var datasetId = Guid.NewGuid();
        var dto = new DatasetResponseDto { Id = datasetId, Name = "D1", UserId = _userId };

        _datasetServiceMock.Setup(s => s.GetByIdAsync(datasetId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var actionResult = await _controller.GetById(datasetId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Update_ValidUpdate_ReturnsOkResult()
    {
        var datasetId = Guid.NewGuid();
        var dto = new DatasetUpdateDto { Name = "Updated" };
        var updated = new DatasetResponseDto { Id = datasetId, Name = "Updated", UserId = _userId };

        _datasetServiceMock.Setup(s => s.UpdateAsync(datasetId, dto, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var actionResult = await _controller.Update(datasetId, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(updated, okResult.Value);
    }

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        var datasetId = Guid.NewGuid();
        _datasetServiceMock.Setup(s => s.DeleteAsync(datasetId, _userId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.Delete(datasetId, CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
        _datasetServiceMock.Verify(s => s.DeleteAsync(datasetId, _userId, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
