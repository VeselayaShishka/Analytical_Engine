using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Analysis;

public class AnalysisRequestDto
{
    [Required]
    public Guid DatasetVersionId { get; set; }

    public string? FilterColumn { get; set; }
    public string? FilterOperator { get; set; }
    public string? FilterValue { get; set; }

    public string? GroupByColumn { get; set; }
    public string? AggregateColumn { get; set; }
    public string? AggregateFunction { get; set; }
}
