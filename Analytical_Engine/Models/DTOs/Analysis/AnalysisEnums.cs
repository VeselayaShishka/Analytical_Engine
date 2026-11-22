using System.Text.Json.Serialization;

namespace Analytical_Engine.Models.DTOs.Analysis;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilterOperator
{
    Eq,
    Neq,
    Gt,
    Gte,
    Lt,
    Lte,
    Contains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AggregateFunction
{
    Count,
    Sum,
    Avg,
    Min,
    Max
}