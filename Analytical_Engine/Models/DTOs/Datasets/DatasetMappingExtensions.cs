using System.Text.Json;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Models.DTOs.Datasets;

public static class DatasetMappingExtensions
{
    public static DatasetResponseDto ToResponseDto(this Dataset dataset, int versionsCount = 0)
    {
        return new DatasetResponseDto
        {
            Id = dataset.Id,
            UserId = dataset.UserId,
            Name = dataset.Name,
            Description = dataset.Description,
            CreatedAt = dataset.CreatedAt,
            ModifiedAt = dataset.ModifiedAt,
            VersionsCount = versionsCount
        };
    }

    public static DatasetVersionResponseDto ToResponseDto(
        this DatasetVersion version,
        bool includeColumns = false)
    {
        return new DatasetVersionResponseDto(
            version.Id,
            version.DatasetId,
            VersionNumber: version.Version,
            SourceFileName: version.FileName,
            FileFormat: version.Format,
            RecordCount: version.RecordCount,
            CreatedAt: version.Created
        );
    }

    public static DatasetColumnResponseDto ToResponseDto(this DatasetColumn column)
    {
        return new DatasetColumnResponseDto
        {
            Id = column.Id,
            Name = column.Name,
            DataType = column.DataType,
            IsNullable = column.IsNullable,
            Position = column.Position,
            Description = column.Description
        };
    }

    public static DataRecordResponseDto ToResponseDto(this DataRecord record)
    {
        var rawJson = string.IsNullOrWhiteSpace(record.Data) ? "{}" : record.Data;
        var parsedData = JsonSerializer.Deserialize<JsonElement>(rawJson);

        return new DataRecordResponseDto
        {
            Id = record.Id,
            RowNumber = record.RowNumber,
            Data = parsedData
        };
    }
}