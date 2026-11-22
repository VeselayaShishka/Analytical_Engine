using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Analysis;

public class AnalysisQueryRequestDto : IValidatableObject
{
    [Required]
    public Guid DatasetVersionId { get; set; }

    [MaxLength(AnalysisLimits.MaxFilters)]
    public List<FilterConditionDto> Filters { get; set; } = new();

    [StringLength(255)]
    public string? GroupBy { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one aggregate function is required.")]
    [MaxLength(AnalysisLimits.MaxAggregates)]
    public List<AggregateRequestDto> Aggregates { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DatasetVersionId == Guid.Empty)
        {
            yield return new ValidationResult(
                "DatasetVersionId cannot be empty.",
                [nameof(DatasetVersionId)]);
        }

        if (GroupBy is not null && string.IsNullOrWhiteSpace(GroupBy))
        {
            yield return new ValidationResult(
                "GroupBy cannot be whitespace.",
                [nameof(GroupBy)]);
        }

        var outputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Aggregates.Count; i++)
        {
            var outputName = Aggregates[i].GetOutputName();
            if (!outputNames.Add(outputName))
            {
                yield return new ValidationResult(
                    $"Duplicate aggregate output name '{outputName}'. Specify a unique Alias.",
                    [$"{nameof(Aggregates)}[{i}]"]);
            }
        }
    }
}