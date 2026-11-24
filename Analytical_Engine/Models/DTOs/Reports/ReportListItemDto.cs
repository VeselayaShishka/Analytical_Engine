namespace Analytical_Engine.Models.DTOs.Reports;

public class ReportListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid DatasetVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
}