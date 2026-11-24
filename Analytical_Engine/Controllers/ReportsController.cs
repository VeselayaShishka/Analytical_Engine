using Analytical_Engine.Extensions;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

/// <summary>
/// Збереження та перегляд аналітичних звітів і результатів розрахунків.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{Roles.Analyst},{Roles.DataAdministrator}")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Збереження нового аналітичного звіту (конфігурація, графік та отримані агрегації).
    /// </summary>
    /// <param name="dto">Параметри звіту (назва, тип, конфігурація та зліпок результатів).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Створений звіт.</returns>
    [HttpPost]
    [RequestSizeLimit(1_048_576)] // Ограничение размера тела запроса (1 МБ)
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] ReportCreateDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _reportService.CreateAsync(
            dto,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    /// <summary>
    /// Отримання пагінованого переліку збережених звітів з можливістю фільтрації за версією або типом звіту.
    /// </summary>
    /// <param name="pageNumber">Номер сторінки (за замовчуванням 1).</param>
    /// <param name="pageSize">Кількість елементів на сторінці (за замовчуванням 20).</param>
    /// <param name="datasetVersionId">Опціональний фільтр за ідентифікатором версії датасету.</param>
    /// <param name="reportType">Опціональний фільтр за типом звіту (Aggregation або Statistics).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Пагінований список звітів.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? datasetVersionId = null,
        [FromQuery] ReportType? reportType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetListAsync(
            User.GetUserId(),
            User.IsDataAdministrator(),
            datasetVersionId,
            reportType,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Отримання повних даних збереженого аналітичного звіту за ідентифікатором.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор звіту.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Детальні дані звіту, включно зі збереженими результатами розрахунку.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetByIdAsync(
            id,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Видалення збереженого звіту.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор звіту.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _reportService.DeleteAsync(
            id,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return NoContent();
    }
}