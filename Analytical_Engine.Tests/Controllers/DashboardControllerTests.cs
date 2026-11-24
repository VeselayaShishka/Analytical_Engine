using System.Security.Claims;
using Analytical_Engine.Controllers;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Dashboard;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock = new();
    private readonly DashboardController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardControllerTests()
    {
        _controller = new DashboardController(_dashboardServiceMock.Object);

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
    public async Task GetSummary_ReturnsOkResultWithSummaryDto()
    {
        var summaryDto = new DashboardSummaryDto
        {
            TotalDatasets = 15,
            TotalVersions = 30,
            TotalRecords = 12000,
            TotalReports = 7,
            RecentDatasets = [],
            RecentReports = []
        };

        _dashboardServiceMock.Setup(s => s.GetSummaryAsync(_userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryDto);

        var actionResult = await _controller.GetSummary(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(summaryDto, okResult.Value);
    }
}
