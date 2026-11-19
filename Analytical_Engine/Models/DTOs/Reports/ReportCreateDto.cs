using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Reports;

public class ReportCreateDto
{
    [Required]
    public Guid DatasetVersionId { get; set; }

    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ReportType { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string ResultData { get; set; } = "{}";
}
