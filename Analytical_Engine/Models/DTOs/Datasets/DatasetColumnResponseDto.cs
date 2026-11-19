namespace Analytical_Engine.Models.DTOs.Datasets;

public class DatasetColumnResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public int Position { get; set; }
    public string? Description { get; set; }
}
