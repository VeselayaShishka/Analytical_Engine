namespace Analytical_Engine.Models.DTOs.Reports;

public class ReportResponseDto
{
    public Guid Id { get; set; }
    public Guid DatasetVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string ResultData { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
}
