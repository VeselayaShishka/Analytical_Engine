using System.Globalization;
using System.Text.Json;

namespace Analytical_Engine.Services;

public readonly record struct NumericValue(bool IsDouble, decimal DecimalValue, double DoubleValue)
{
    public double AsDouble() => IsDouble ? DoubleValue : (double)DecimalValue;
}

public class ColumnValueConverter
{
    private static readonly HashSet<string> NumericTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "integer", "long", "decimal", "double"
    };

    private static readonly HashSet<string> ExactDecimalTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "integer", "long", "decimal"
    };

    public bool IsNumericType(string dataType) => NumericTypes.Contains(dataType);

    public bool IsExactDecimalType(string dataType) => ExactDecimalTypes.Contains(dataType);

    public bool IsDateTimeType(string dataType) =>
        string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase);

    public bool IsStringType(string dataType) =>
        string.Equals(dataType, "string", StringComparison.OrdinalIgnoreCase);

    public bool IsBooleanType(string dataType) =>
        string.Equals(dataType, "bool", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dataType, "boolean", StringComparison.OrdinalIgnoreCase);

    public bool TryConvert(JsonElement element, string dataType, out object? value)
    {
        value = null;

        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        switch (dataType.ToLowerInvariant())
        {
            case "string":
                if (element.ValueKind == JsonValueKind.String)
                {
                    value = element.GetString();
                    return true;
                }
                if (element.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    value = element.GetRawText();
                    return true;
                }
                return false;

            case "int" or "integer":
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intVal))
                {
                    value = intVal;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.String &&
                    int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out intVal))
                {
                    value = intVal;
                    return true;
                }
                return false;

            case "long":
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var longVal))
                {
                    value = longVal;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.String &&
                    long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out longVal))
                {
                    value = longVal;
                    return true;
                }
                return false;

            case "decimal":
                if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var decVal))
                {
                    value = decVal;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decVal))
                {
                    value = decVal;
                    return true;
                }
                return false;

            case "double":
                if (element.ValueKind == JsonValueKind.Number &&
                    element.TryGetDouble(out var dblVal) &&
                    !double.IsNaN(dblVal) && !double.IsInfinity(dblVal))
                {
                    value = dblVal;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.String &&
                    double.TryParse(element.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out dblVal) &&
                    !double.IsNaN(dblVal) && !double.IsInfinity(dblVal))
                {
                    value = dblVal;
                    return true;
                }
                return false;

            case "bool" or "boolean":
                if (element.ValueKind == JsonValueKind.True)
                {
                    value = true;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.False)
                {
                    value = false;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.String &&
                    bool.TryParse(element.GetString(), out var boolVal))
                {
                    value = boolVal;
                    return true;
                }
                return false;

            case "datetime":
                if (element.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtVal))
                {
                    value = dtVal.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dtVal, DateTimeKind.Utc)
                        : dtVal.ToUniversalTime();
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    public bool TryConvertToNumeric(JsonElement element, string dataType, out NumericValue numericValue)
    {
        numericValue = default;

        if (!TryConvert(element, dataType, out var converted) || converted is null)
        {
            return false;
        }

        if (IsExactDecimalType(dataType))
        {
            decimal dec = converted switch
            {
                int i => i,
                long l => l,
                decimal d => d,
                _ => Convert.ToDecimal(converted, CultureInfo.InvariantCulture)
            };

            numericValue = new NumericValue(IsDouble: false, DecimalValue: dec, DoubleValue: (double)dec);
            return true;
        }

        if (converted is double dbl)
        {
            numericValue = new NumericValue(IsDouble: true, DecimalValue: 0m, DoubleValue: dbl);
            return true;
        }

        return false;
    }

    public bool IsValidRawValue(object value, string expectedType)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) &&
                   TryConvert(jsonElement, expectedType, out _);
        }

        if (value is bool)
        {
            return IsBooleanType(expectedType) || IsStringType(expectedType);
        }

        var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(stringValue));
        return TryConvert(doc.RootElement, expectedType, out var parsed) && parsed is not null;
    }
}