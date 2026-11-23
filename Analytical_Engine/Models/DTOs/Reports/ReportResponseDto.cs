using System.Text.Json;

namespace Analytical_Engine.Models.DTOs.Reports;

public class ReportResponseDto
{
    public Guid Id { get; set; }
    public Guid DatasetVersionId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public JsonElement ResultData { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
}
