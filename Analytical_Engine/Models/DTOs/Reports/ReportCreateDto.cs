using System.ComponentModel.DataAnnotations;
using Analytical_Engine.Models.DTOs.Analysis;

namespace Analytical_Engine.Models.DTOs.Reports;

public class ReportCreateDto : IValidatableObject
{
    [Required]
    public Guid DatasetVersionId { get; set; }

    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ReportType ReportType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public AnalysisQueryRequestDto? QueryRequest { get; set; }

    public StatisticsRequestDto? StatisticsRequest { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DatasetVersionId == Guid.Empty)
        {
            yield return new ValidationResult(
                "DatasetVersionId cannot be empty.",
                [nameof(DatasetVersionId)]);
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Report name cannot be empty or whitespace.",
                [nameof(Name)]);
        }

        if (QueryRequest is not null && StatisticsRequest is not null)
        {
            yield return new ValidationResult(
                "Provide either QueryRequest or StatisticsRequest, not both.",
                [nameof(QueryRequest), nameof(StatisticsRequest)]);
            yield break;
        }

        if (QueryRequest is null && StatisticsRequest is null)
        {
            yield return new ValidationResult(
                "Either QueryRequest or StatisticsRequest must be provided to generate a report.",
                [nameof(QueryRequest), nameof(StatisticsRequest)]);
            yield break;
        }

        switch (ReportType)
        {
            case ReportType.Aggregation:
                if (QueryRequest is null)
                {
                    yield return new ValidationResult(
                        "ReportType 'Aggregation' requires QueryRequest.",
                        [nameof(QueryRequest)]);
                }
                else if (!string.IsNullOrWhiteSpace(QueryRequest.GroupBy))
                {
                    yield return new ValidationResult(
                        "ReportType 'Aggregation' cannot contain GroupBy. Use 'Grouping' instead.",
                        [$"{nameof(QueryRequest)}.{nameof(QueryRequest.GroupBy)}"]);
                }
                break;

            case ReportType.Grouping:
                if (QueryRequest is null)
                {
                    yield return new ValidationResult(
                        "ReportType 'Grouping' requires QueryRequest.",
                        [nameof(QueryRequest)]);
                }
                else if (string.IsNullOrWhiteSpace(QueryRequest.GroupBy))
                {
                    yield return new ValidationResult(
                        "ReportType 'Grouping' requires GroupBy to be specified in QueryRequest.",
                        [$"{nameof(QueryRequest)}.{nameof(QueryRequest.GroupBy)}"]);
                }
                break;

            case ReportType.Statistics:
                if (StatisticsRequest is null)
                {
                    yield return new ValidationResult(
                        "ReportType 'Statistics' requires StatisticsRequest.",
                        [nameof(StatisticsRequest)]);
                }
                break;

            case ReportType.Custom:
                break;
        }
    }
}