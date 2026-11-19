namespace Analytical_Engine.Models.Entities;

public class Report
{
    public Guid Id { get; set; }
    public Guid DatasetVersionId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string ResultData { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public DatasetVersion DatasetVersion { get; set; } = null!;
    public User User { get; set; } = null!;
}