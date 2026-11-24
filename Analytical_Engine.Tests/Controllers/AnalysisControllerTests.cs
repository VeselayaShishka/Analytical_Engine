using System.Security.Claims;
using Analytical_Engine.Controllers;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Controllers;

public class AnalysisControllerTests
{
    private readonly Mock<IAnalysisService> _analysisServiceMock = new();
    private readonly AnalysisController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public AnalysisControllerTests()
    {
        _controller = new AnalysisController(_analysisServiceMock.Object);

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
    public async Task ExecuteQuery_ValidRequest_ReturnsOkResult()
    {
        var request = new AnalysisQueryRequestDto { DatasetVersionId = Guid.NewGuid() };
        var response = new AnalysisResultDto<IReadOnlyList<GroupResultItem>>
        {
            DatasetVersionId = request.DatasetVersionId,
            Operation = "sum",
            RecordsProcessed = 42,
            Result = []
        };

        _analysisServiceMock.Setup(s => s.ExecuteAsync(request, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _controller.ExecuteQuery(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetStatistics_ValidRequest_ReturnsOkResult()
    {
        var request = new StatisticsRequestDto { DatasetVersionId = Guid.NewGuid(), Column = "price" };
        var response = new AnalysisResultDto<IReadOnlyList<StatisticsResult>>
        {
            DatasetVersionId = request.DatasetVersionId,
            Operation = "statistics(price)",
            RecordsProcessed = 10,
            Result = []
        };

        _analysisServiceMock.Setup(s => s.GetStatisticsAsync(request, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _controller.GetStatistics(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(response, okResult.Value);
    }
}
