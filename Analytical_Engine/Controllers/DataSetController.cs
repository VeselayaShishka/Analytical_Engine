using Analytical_Engine.Extensions;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Datasets;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

/// <summary>
/// Керування наборами відкритих даних, версіями, колонками та записами.
/// </summary>
[ApiController]
[Route("api/datasets")]
[Authorize(Roles = $"{Roles.Analyst},{Roles.DataAdministrator}")]
public class DatasetsController : ControllerBase
{
    private readonly IDatasetService _datasetService;

    public DatasetsController(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    /// <summary>
    /// Створення нового набору відкритих даних.
    /// </summary>
    /// <param name="dto">Метадані створюваного датасету.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Інформація про створений датасет.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DatasetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] DatasetCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var created = await _datasetService.CreateAsync(dto, userId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    /// <summary>
    /// Отримання пагінованого списку датасетів з пошуком.
    /// </summary>
    /// <param name="search">Пошуковий рядок (назва або опис).</param>
    /// <param name="pageNumber">Номер сторінки (за замовчуванням 1).</param>
    /// <param name="pageSize">Розмір сторінки (за замовчуванням 20).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Пагінований список датасетів.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DatasetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _datasetService.GetListAsync(
            User.GetUserId(),
            User.IsDataAdministrator(),
            search,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Отримання детальних даних конкретного набору даних за його ідентифікатором.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Дані набору даних.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DatasetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _datasetService.GetByIdAsync(
            id,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Оновлення інформації про набір даних (назва, опис).
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="dto">Нові дані для оновлення.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Оновлений набір даних.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DatasetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] DatasetUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _datasetService.UpdateAsync(
            id,
            dto,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Видалення набору даних (доступно автору або DataAdministrator).
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _datasetService.DeleteAsync(
            id,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Отримання списку версій для обраного набору даних.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Список версій датасету.</returns>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<DatasetVersionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _datasetService.GetVersionsAsync(
            id,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Отримання схеми колонок та їх типів для конкретної версії набору даних.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="versionId">Унікальний ідентифікатор версії.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Список колонок версії датасету.</returns>
    [HttpGet("{id:guid}/versions/{versionId:guid}/columns")]
    [ProducesResponseType(typeof(IReadOnlyList<DatasetColumnResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetColumns(
        Guid id,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _datasetService.GetColumnsAsync(
            id,
            versionId,
            User.GetUserId(),
            User.IsDataAdministrator(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Отримання сирих записів обраної версії набору даних з пагінацією.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор набору даних.</param>
    /// <param name="versionId">Унікальний ідентифікатор версії.</param>
    /// <param name="pageNumber">Номер сторінки.</param>
    /// <param name="pageSize">Кількість рядків на сторінці.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <returns>Пагінована вибірка записів даних.</returns>
    [HttpGet("{id:guid}/versions/{versionId:guid}/records")]
    [ProducesResponseType(typeof(PagedResultDto<DataRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecords(
        Guid id,
        Guid versionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _datasetService.GetRecordsAsync(
            id,
            versionId,
            User.GetUserId(),
            User.IsDataAdministrator(),
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }
}