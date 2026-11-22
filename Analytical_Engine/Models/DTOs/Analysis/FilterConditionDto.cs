using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Analytical_Engine.Models.DTOs.Analysis;

public class FilterConditionDto : IValidatableObject
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Column { get; set; } = string.Empty;

    [Required]
    public FilterOperator Operator { get; set; }

    public JsonElement? Value { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Column))
        {
            yield return new ValidationResult(
                "Filter column name cannot be empty or whitespace.",
                [nameof(Column)]);
        }

        var hasNonNullValue = Value.HasValue &&
                              Value.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

        if (Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
        {
            if (hasNonNullValue)
            {
                yield return new ValidationResult(
                    $"Filter value must not be provided for operator '{Operator}'.",
                    [nameof(Value)]);
            }
        }
        else if (!hasNonNullValue)
        {
            yield return new ValidationResult(
                $"Filter value is required for operator '{Operator}'.",
                [nameof(Value)]);
        }
    }
}