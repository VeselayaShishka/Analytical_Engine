using System.Text.Json.Serialization;

namespace Analytical_Engine.Models.DTOs.Reports;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportType
{
    Aggregation,
    Grouping,
    Statistics,
    Custom
}