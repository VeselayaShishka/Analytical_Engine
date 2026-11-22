using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Analysis;

public class AggregateRequestDto : IValidatableObject
{
    [Required]
    public AggregateFunction Function { get; set; }

    [StringLength(255)]
    public string? Column { get; set; }

    [StringLength(100)]
    public string? Alias { get; set; }

    public string GetOutputName()
    {
        if (!string.IsNullOrWhiteSpace(Alias))
        {
            return Alias.Trim();
        }

        var funcName = Function.ToString().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(Column)
            ? $"{funcName}(*)"
            : $"{funcName}({Column.Trim()})";
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Function != AggregateFunction.Count && string.IsNullOrWhiteSpace(Column))
        {
            yield return new ValidationResult(
                $"Column is required for aggregate function '{Function}'.",
                [nameof(Column)]);
        }
    }
}