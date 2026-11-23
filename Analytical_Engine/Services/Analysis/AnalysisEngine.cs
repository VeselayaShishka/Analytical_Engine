using System.Text.Json;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Services.Analysis;

public sealed record QueryExecutionOutput(
    long RecordsProcessed,
    IReadOnlyList<GroupResultItem> Groups);

public sealed record StatisticsExecutionOutput(
    long RecordsProcessed,
    IReadOnlyList<StatisticsResult> Statistics);

public class AnalysisEngine
{
    private readonly ColumnValueConverter _converter;

    public AnalysisEngine(ColumnValueConverter converter)
    {
        _converter = converter;
    }

    public async Task<QueryExecutionOutput> ExecuteQueryAsync(
        IAsyncEnumerable<string> jsonRows,
        ValidatedQueryPlan plan,
        CancellationToken cancellationToken = default)
    {
        var groups = new Dictionary<GroupKey, GroupAccumulatorSet>();
        long recordsProcessed = 0;
        long totalRead = 0;

        await foreach (var rawJson in jsonRows.WithCancellation(cancellationToken))
        {
            totalRead++;
            if (totalRead > AnalysisLimits.MaxProcessedRecords)
            {
                throw new RequestValidationException(
                    "DatasetVersionId",
                    $"Maximum number of processed records ({AnalysisLimits.MaxProcessedRecords}) exceeded. Apply filters or process a smaller dataset.");
            }

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!MatchesAllFilters(root, plan.Filters))
            {
                continue;
            }

            recordsProcessed++;

            var groupKey = ExtractGroupKey(root, plan.GroupByColumn);
            if (!groups.TryGetValue(groupKey, out var accumulatorSet))
            {
                if (groups.Count >= AnalysisLimits.MaxGroups)
                {
                    throw new RequestValidationException(
                        nameof(AnalysisQueryRequestDto.GroupBy),
                        $"Maximum number of distinct groups ({AnalysisLimits.MaxGroups}) exceeded.");
                }

                accumulatorSet = new GroupAccumulatorSet(plan.Aggregates, _converter);
                groups[groupKey] = accumulatorSet;
            }

            accumulatorSet.AccumulateRow(root);
        }

        if (groups.Count == 0 && plan.GroupByColumn is null)
        {
            var emptySet = new GroupAccumulatorSet(plan.Aggregates, _converter);
            return new QueryExecutionOutput(
                RecordsProcessed: 0,
                Groups:
                [
                    new GroupResultItem
                    {
                        Key = null,
                        Values = emptySet.BuildValues(),
                        Count = 0
                    }
                ]);
        }

        var sortedGroups = groups
            .OrderBy(kvp => kvp.Key.IsNull ? 0 : 1)
            .ThenBy(kvp => kvp.Key.Key, StringComparer.Ordinal)
            .Select(kvp => new GroupResultItem
            {
                Key = kvp.Key.Key,
                Values = kvp.Value.BuildValues(),
                Count = kvp.Value.TotalCount
            })
            .ToList();

        return new QueryExecutionOutput(recordsProcessed, sortedGroups);
    }

    public async Task<StatisticsExecutionOutput> ExecuteStatisticsAsync(
        IAsyncEnumerable<string> jsonRows,
        ValidatedStatisticsPlan plan,
        CancellationToken cancellationToken = default)
    {
        var groups = new Dictionary<GroupKey, WelfordStatisticsAccumulator>();
        long recordsProcessed = 0;
        long totalRead = 0;

        await foreach (var rawJson in jsonRows.WithCancellation(cancellationToken))
        {
            totalRead++;
            if (totalRead > AnalysisLimits.MaxProcessedRecords)
            {
                throw new RequestValidationException(
                    "DatasetVersionId",
                    $"Maximum number of processed records ({AnalysisLimits.MaxProcessedRecords}) exceeded.");
            }

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!MatchesAllFilters(root, plan.Filters))
            {
                continue;
            }

            recordsProcessed++;

            var groupKey = ExtractGroupKey(root, plan.GroupByColumn);
            if (!groups.TryGetValue(groupKey, out var statsAccumulator))
            {
                if (groups.Count >= AnalysisLimits.MaxGroups)
                {
                    throw new RequestValidationException(
                        nameof(StatisticsRequestDto.GroupBy),
                        $"Maximum number of distinct groups ({AnalysisLimits.MaxGroups}) exceeded.");
                }

                statsAccumulator = new WelfordStatisticsAccumulator(plan.TargetColumn.DataType, _converter);
                groups[groupKey] = statsAccumulator;
            }

            var targetCell = JsonRowHelper.GetPropertyCaseInsensitive(root, plan.TargetColumn.Name);
            statsAccumulator.Accumulate(targetCell);
        }

        if (groups.Count == 0 && plan.GroupByColumn is null)
        {
            var emptyStats = new WelfordStatisticsAccumulator(plan.TargetColumn.DataType, _converter);
            return new StatisticsExecutionOutput(
                RecordsProcessed: 0,
                Statistics: [emptyStats.BuildResult(groupKey: null)]);
        }

        var sortedResults = groups
            .OrderBy(kvp => kvp.Key.IsNull ? 0 : 1)
            .ThenBy(kvp => kvp.Key.Key, StringComparer.Ordinal)
            .Select(kvp => kvp.Value.BuildResult(kvp.Key.Key))
            .ToList();

        return new StatisticsExecutionOutput(recordsProcessed, sortedResults);
    }

    private GroupKey ExtractGroupKey(JsonElement root, DatasetColumn? groupByColumn)
    {
        if (groupByColumn is null)
        {
            return new GroupKey(IsNull: true, Key: null);
        }

        var cell = JsonRowHelper.GetPropertyCaseInsensitive(root, groupByColumn.Name);
        if (!cell.HasValue || cell.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new GroupKey(IsNull: true, Key: null);
        }

        if (!_converter.TryConvert(cell.Value, groupByColumn.DataType, out var converted) || converted is null)
        {
            return new GroupKey(IsNull: true, Key: null);
        }

        return new GroupKey(IsNull: false, Key: JsonRowHelper.FormatGroupKey(converted));
    }

    private bool MatchesAllFilters(JsonElement root, IReadOnlyList<ValidatedFilter> filters)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            if (!EvaluateFilter(root, filters[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateFilter(JsonElement root, ValidatedFilter filter)
    {
        var cell = JsonRowHelper.GetPropertyCaseInsensitive(root, filter.ColumnName);
        var isCellNull = !cell.HasValue ||
                         cell.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        if (filter.Operator == FilterOperator.IsNull)
        {
            return isCellNull;
        }

        if (filter.Operator == FilterOperator.IsNotNull)
        {
            return !isCellNull;
        }

        if (isCellNull)
        {
            return false;
        }

        if (!_converter.TryConvert(cell!.Value, filter.DataType, out var actualValue) || actualValue is null)
        {
            return false;
        }

        var expectedValue = filter.ConvertedValue!;

        if (actualValue is string actualStr && expectedValue is string expectedStr)
        {
            return filter.Operator switch
            {
                FilterOperator.Eq => string.Equals(actualStr, expectedStr, StringComparison.Ordinal),
                FilterOperator.Neq => !string.Equals(actualStr, expectedStr, StringComparison.Ordinal),
                FilterOperator.Contains => actualStr.Contains(expectedStr, StringComparison.OrdinalIgnoreCase),
                FilterOperator.StartsWith => actualStr.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase),
                FilterOperator.EndsWith => actualStr.EndsWith(expectedStr, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        if (actualValue is bool actualBool && expectedValue is bool expectedBool)
        {
            return filter.Operator switch
            {
                FilterOperator.Eq => actualBool == expectedBool,
                FilterOperator.Neq => actualBool != expectedBool,
                _ => false
            };
        }

        var comparison = CompareValues(actualValue, expectedValue);

        return filter.Operator switch
        {
            FilterOperator.Eq => comparison == 0,
            FilterOperator.Neq => comparison != 0,
            FilterOperator.Gt => comparison > 0,
            FilterOperator.Gte => comparison >= 0,
            FilterOperator.Lt => comparison < 0,
            FilterOperator.Lte => comparison <= 0,
            _ => false
        };
    }

    private static int CompareValues(object actual, object expected)
    {
        return (actual, expected) switch
        {
            (int a, int b) => a.CompareTo(b),
            (long a, long b) => a.CompareTo(b),
            (decimal a, decimal b) => a.CompareTo(b),
            (double a, double b) => a.CompareTo(b),
            (DateTime a, DateTime b) => a.CompareTo(b),
            _ => Comparer<object>.Default.Compare(actual, expected)
        };
    }

    private readonly record struct GroupKey(bool IsNull, string? Key);
}