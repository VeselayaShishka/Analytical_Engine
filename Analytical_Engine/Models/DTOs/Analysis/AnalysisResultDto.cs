namespace Analytical_Engine.Models.DTOs.Analysis;

public class AnalysisResultDto<T>
{
    public Guid DatasetVersionId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public long RecordsProcessed { get; set; }
    public T Result { get; set; } = default!;
}

public class GroupResultItem
{
    public string? Key { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
    public long Count { get; set; }
}

public class StatisticsResult
{
    public string? Key { get; set; }
    public long Count { get; set; }
    public long NullCount { get; set; }
    public object? Sum { get; set; }
    public double? Mean { get; set; }
    public object? Min { get; set; }
    public object? Max { get; set; }
    public double? Median { get; set; }
    public double? Variance { get; set; }
    public double? StdDev { get; set; }
    public double? Q1 { get; set; }
    public double? Q3 { get; set; }
    public double? Iqr { get; set; }
}