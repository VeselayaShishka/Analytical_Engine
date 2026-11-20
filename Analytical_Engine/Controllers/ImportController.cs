using System.Security.Claims;
using System.Text.Json;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Imports;
using Analytical_Engine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analytical_Engine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Analyst},{Roles.DataAdministrator}")]
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

        Schema? schema;

        try
        {
            schema = JsonSerializer.Deserialize<Schema>(
                request.SchemaJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid schema JSON.");
        }

        if (schema?.Fields == null || schema.Fields.Count == 0)
            return BadRequest("Schema cannot be empty.");

        var supportedTypes = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "string",
            "int",
            "integer",
            "long",
            "decimal",
            "double",
            "bool",
            "boolean",
            "datetime"
        };

        foreach (var field in schema.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key))
                return BadRequest("Schema contains an empty field name.");

            if (field.Value == null ||
                string.IsNullOrWhiteSpace(field.Value.Type))
            {
                return BadRequest(
                    $"Type is not specified for field '{field.Key}'.");
            }

            if (!supportedTypes.Contains(field.Value.Type))
            {
                return BadRequest(
                    $"Unsupported type '{field.Value.Type}' " +
                    $"for field '{field.Key}'.");
            }
        }
        
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