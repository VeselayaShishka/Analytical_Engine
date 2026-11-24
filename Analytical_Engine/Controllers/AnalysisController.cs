using Analytical_Engine.Extensions;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

/// <summary>
/// Аналітичний рушій для групування, фільтрації та розрахунку описової статистики.
/// </summary>
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

    /// <summary>
    /// Виконання аналітичного агрегаційного запиту (Group By, Count, Sum, Avg, Min, Max) над даними версії.
    /// </summary>
    /// <param name="request">Параметри запиту: фільтри, колонка групування, агрегатні функції, пагінація.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Результати аналізу з числовими даними та метаданими виконання.</returns>
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

    /// <summary>
    /// Розрахунок повної описової статистики (середнє, дисперсія, СКВ за алгоритмом Велфорда, медіана, мін, макс) для числових колонок.
    /// </summary>
    /// <param name="request">Параметри розрахунку: ідентифікатор версії датасету, цільова колонка, фільтри.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Статистичні показники розподілу даних.</returns>
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