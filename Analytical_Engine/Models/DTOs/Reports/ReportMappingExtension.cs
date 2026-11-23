using System.Text.Json;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Models.DTOs.Reports;

public static class ReportMappingExtensions
{
    public static ReportResponseDto ToResponseDto(this Report report)
    {
        var rawJson = string.IsNullOrWhiteSpace(report.ResultData) ? "{}" : report.ResultData;
        var parsedData = JsonSerializer.Deserialize<JsonElement>(rawJson);

        return new ReportResponseDto
        {
            Id = report.Id,
            DatasetVersionId = report.DatasetVersionId,
            UserId = report.UserId,
            Name = report.Name,
            ReportType = report.ReportType,
            ResultData = parsedData,
            CreatedAt = report.CreatedAt,
            Description = report.Description
        };
    }
}