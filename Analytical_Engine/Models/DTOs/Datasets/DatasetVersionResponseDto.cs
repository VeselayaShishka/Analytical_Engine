namespace Analytical_Engine.Models.DTOs.Datasets;

public record DatasetVersionResponseDto(
    Guid Id,
    Guid DatasetId,
    int VersionNumber,
    string SourceFileName,
    string FileFormat,
    long RecordCount,
    DateTime CreatedAt);