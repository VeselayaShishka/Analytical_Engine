using System.Security.Claims;
using System.Text.Json;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Imports;
using Analytical_Engine.Services;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly CSVImportService _csvImportService;
    private readonly JSONImportService _jsonImportService;

    public ImportController(
        CSVImportService csvImportService,
        JSONImportService jsonImportService)
    {
        _csvImportService = csvImportService;
        _jsonImportService = jsonImportService;
    }

    [HttpPost("dataset")]
    public async Task<IActionResult> Import(
        [FromForm] ImportDatasetRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("File wasn't loaded.");

        var extension = Path.GetExtension(request.File.FileName)
            .ToLowerInvariant();

        IImportService importService = extension switch
        {
            ".csv" => _csvImportService,
            ".json" => _jsonImportService,
            _ => null!
        };

        if (importService == null)
            return StatusCode(415, "File type is not supported.");

        if (string.IsNullOrWhiteSpace(request.SchemaJson))
            return BadRequest("Schema is required.");

        Dictionary<string, string>? fields;

        try
        {
            fields = JsonSerializer.Deserialize<Dictionary<string, string>>(
                request.SchemaJson);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid schema JSON.");
        }

        if (fields == null || fields.Count == 0)
            return BadRequest("Schema cannot be empty.");

        var schema = new Schema(fields);

        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var metadata = new Metadata(
            request.DatasetId,
            request.File.FileName,
            email,
            extension.TrimStart('.').ToUpperInvariant(),
            DateTime.UtcNow
        );

        await using var stream = request.File.OpenReadStream();

        var result = await importService.ImportDataset(
            stream,
            schema,
            metadata);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}