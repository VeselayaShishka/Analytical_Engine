using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Analysis;

public class StatisticsRequestDto : IValidatableObject
{
    [Required]
    public Guid DatasetVersionId { get; set; }

    [Required, StringLength(255, MinimumLength = 1)]
    public string Column { get; set; } = string.Empty;

    [MaxLength(AnalysisLimits.MaxFilters)]
    public List<FilterConditionDto> Filters { get; set; } = new();

    [StringLength(255)]
    public string? GroupBy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DatasetVersionId == Guid.Empty)
        {
            yield return new ValidationResult(
                "DatasetVersionId cannot be empty.",
                [nameof(DatasetVersionId)]);
        }

        if (string.IsNullOrWhiteSpace(Column))
        {
            yield return new ValidationResult(
                "Target column cannot be empty or whitespace.",
                [nameof(Column)]);
        }

        if (GroupBy is not null && string.IsNullOrWhiteSpace(GroupBy))
        {
            yield return new ValidationResult(
                "GroupBy cannot be whitespace.",
                [nameof(GroupBy)]);
        }
    }
}