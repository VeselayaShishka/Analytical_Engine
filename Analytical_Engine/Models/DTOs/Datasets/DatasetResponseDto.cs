namespace Analytical_Engine.Models.DTOs.Datasets;

public class DatasetResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int VersionsCount { get; set; }
}
