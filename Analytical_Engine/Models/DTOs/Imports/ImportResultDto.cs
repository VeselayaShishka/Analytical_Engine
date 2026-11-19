namespace Analytical_Engine.Models.DTOs.Imports;

public class ImportResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? DatasetVersionId { get; set; }
    public int? Version { get; set; }
    public long ImportedRecords { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}
