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
    private const long MaxImportFileSizeBytes = 52_428_800; // 50 MB

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
    [RequestSizeLimit(MaxImportFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportFileSizeBytes)]
    public async Task<IActionResult> Import(
        [FromForm] ImportDatasetRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "File wasn't loaded.");
        }

        var extension = Path.GetExtension(request.File.FileName)
            .ToLowerInvariant();

        IImportService? importService = extension switch
        {
            ".csv" => _csvImportService,
            ".json" => _jsonImportService,
            _ => null
        };

        if (importService == null)
        {
            return Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType,
                title: "Unsupported Media Type",
                detail: "File type is not supported. Only .csv and .json files are allowed.");
        }

        if (string.IsNullOrWhiteSpace(request.SchemaJson))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Schema is required.");
        }

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
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Invalid schema JSON.");
        }

        if (schema?.Fields == null || schema.Fields.Count == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Schema cannot be empty.");
        }

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
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Schema contains an empty field name.");
            }

            if (field.Value == null || string.IsNullOrWhiteSpace(field.Value.Type))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: $"Type is not specified for field '{field.Key}'.");
            }

            if (!supportedTypes.Contains(field.Value.Type))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: $"Unsupported type '{field.Value.Type}' for field '{field.Key}'.");
            }
        }

        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User email claim is missing.");
        }

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
        {
            if (result.Errors.Count > 0)
            {
                return BadRequest(result);
            }

            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Import Error",
                detail: result.Message);
        }

        return Ok(result);
    }
}