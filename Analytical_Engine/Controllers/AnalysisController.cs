using Analytical_Engine.Extensions;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize(Roles = $"{Roles.Analyst},{Roles.DataAdministrator}")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;

    public AnalysisController(IAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(AnalysisResultDto<IReadOnlyList<GroupResultItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] AnalysisQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _analysisService.ExecuteAsync(
            request,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("statistics")]
    [ProducesResponseType(typeof(AnalysisResultDto<IReadOnlyList<StatisticsResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(
        [FromBody] StatisticsRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _analysisService.GetStatisticsAsync(
            request,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }
}