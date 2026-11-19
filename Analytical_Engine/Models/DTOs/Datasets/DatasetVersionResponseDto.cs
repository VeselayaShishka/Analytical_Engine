namespace Analytical_Engine.Models.DTOs.Datasets;

public class DatasetVersionResponseDto
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public int Version { get; set; }
    public string Format { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long RecordCount { get; set; }
    public DateTime Created { get; set; }
    public List<DatasetColumnResponseDto> Columns { get; set; } = new();
}
