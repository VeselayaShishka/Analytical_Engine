using System.Security.Claims;
using Analytical_Engine.Controllers;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock = new();
    private readonly ReportsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ReportsControllerTests()
    {
        _controller = new ReportsController(_reportServiceMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, Roles.Analyst)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Create_ValidReport_ReturnsCreatedAtActionResult()
    {
        var dto = new ReportCreateDto { Name = "Monthly Revenue", DatasetVersionId = Guid.NewGuid() };
        var created = new ReportResponseDto { Id = Guid.NewGuid(), Name = "Monthly Revenue", UserId = _userId };

        _reportServiceMock.Setup(s => s.CreateAsync(dto, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var actionResult = await _controller.Create(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(ReportsController.GetById), createdResult.ActionName);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task GetList_ReturnsPagedList()
    {
        var paged = new PagedResultDto<ReportListItemDto>
        {
            Items = [new ReportListItemDto { Id = Guid.NewGuid(), Name = "Report1" }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };

        _reportServiceMock.Setup(s => s.GetListAsync(_userId, false, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var actionResult = await _controller.GetList(1, 20, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(paged, okResult.Value);
    }

    [Fact]
    public async Task GetById_ExistingReport_ReturnsOkResult()
    {
        var reportId = Guid.NewGuid();
        var dto = new ReportResponseDto { Id = reportId, Name = "Report1", UserId = _userId };

        _reportServiceMock.Setup(s => s.GetByIdAsync(reportId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var actionResult = await _controller.GetById(reportId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        var reportId = Guid.NewGuid();
        _reportServiceMock.Setup(s => s.DeleteAsync(reportId, _userId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.Delete(reportId, CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
        _reportServiceMock.Verify(s => s.DeleteAsync(reportId, _userId, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
