using System.Text.Json;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Services.Analysis;

public sealed record ValidatedFilter(
    string ColumnName,
    string DataType,
    FilterOperator Operator,
    object? ConvertedValue);

public sealed record ValidatedAggregate(
    string OutputName,
    AggregateFunction Function,
    string? ColumnName,
    string? DataType);

public sealed record ValidatedQueryPlan(
    IReadOnlyList<ValidatedFilter> Filters,
    DatasetColumn? GroupByColumn,
    IReadOnlyList<ValidatedAggregate> Aggregates);

public sealed record ValidatedStatisticsPlan(
    DatasetColumn TargetColumn,
    IReadOnlyList<ValidatedFilter> Filters,
    DatasetColumn? GroupByColumn);

public class AnalysisRequestValidator
{
    private readonly ColumnValueConverter _converter;

    public AnalysisRequestValidator(ColumnValueConverter converter)
    {
        _converter = converter;
    }

    public ValidatedQueryPlan ValidateQuery(
        AnalysisQueryRequestDto request,
        IEnumerable<DatasetColumn> columns)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var columnMap = columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var validatedFilters = ValidateFilters(request.Filters, columnMap, errors);
        var groupByColumn = ValidateGroupBy(request.GroupBy, columnMap, errors);
        var validatedAggregates = ValidateAggregates(request.Aggregates, columnMap, errors);

        ThrowIfHasErrors(errors);

        return new ValidatedQueryPlan(validatedFilters, groupByColumn, validatedAggregates);
    }

    public ValidatedStatisticsPlan ValidateStatistics(
        StatisticsRequestDto request,
        IEnumerable<DatasetColumn> columns)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var columnMap = columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        DatasetColumn? targetColumn = null;
        var targetName = request.Column.Trim();

        if (!columnMap.TryGetValue(targetName, out targetColumn))
        {
            AddError(errors, nameof(request.Column), $"Column '{targetName}' does not exist in this dataset version.");
        }
        else if (!_converter.IsNumericType(targetColumn.DataType))
        {
            AddError(errors, nameof(request.Column),
                $"Statistics can only be computed for numeric columns. Column '{targetColumn.Name}' has type '{targetColumn.DataType}'.");
        }

        var validatedFilters = ValidateFilters(request.Filters, columnMap, errors);
        var groupByColumn = ValidateGroupBy(request.GroupBy, columnMap, errors);

        ThrowIfHasErrors(errors);

        return new ValidatedStatisticsPlan(targetColumn!, validatedFilters, groupByColumn);
    }

    private List<ValidatedFilter> ValidateFilters(
        IReadOnlyList<FilterConditionDto> filters,
        Dictionary<string, DatasetColumn> columnMap,
        Dictionary<string, List<string>> errors)
    {
        var result = new List<ValidatedFilter>(filters.Count);

        for (var i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            var prefix = $"Filters[{i}]";
            var colName = filter.Column.Trim();

            if (!columnMap.TryGetValue(colName, out var column))
            {
                AddError(errors, $"{prefix}.Column", $"Column '{colName}' does not exist in this dataset version.");
                continue;
            }

            if (!IsOperatorCompatible(filter.Operator, column.DataType))
            {
                AddError(errors, $"{prefix}.Operator",
                    $"Operator '{filter.Operator}' is not supported for column '{column.Name}' of type '{column.DataType}'.");
                continue;
            }

            if (filter.Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
            {
                if (filter.Value.HasValue &&
                    filter.Value.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                {
                    AddError(errors, $"{prefix}.Value",
                        $"Filter value must be omitted for operator '{filter.Operator}'.");
                    continue;
                }

                result.Add(new ValidatedFilter(column.Name, column.DataType, filter.Operator, null));
                continue;
            }

            if (!filter.Value.HasValue ||
                filter.Value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                AddError(errors, $"{prefix}.Value",
                    $"Filter value is required for operator '{filter.Operator}'.");
                continue;
            }

            if (!_converter.TryConvert(filter.Value.Value, column.DataType, out var convertedValue) ||
                convertedValue is null)
            {
                AddError(errors, $"{prefix}.Value",
                    $"Value '{filter.Value.Value.GetRawText()}' cannot be converted to column type '{column.DataType}'.");
                continue;
            }

            result.Add(new ValidatedFilter(column.Name, column.DataType, filter.Operator, convertedValue));
        }

        return result;
    }

    private static DatasetColumn? ValidateGroupBy(
        string? groupBy,
        Dictionary<string, DatasetColumn> columnMap,
        Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
        {
            return null;
        }

        var colName = groupBy.Trim();
        if (!columnMap.TryGetValue(colName, out var column))
        {
            AddError(errors, nameof(AnalysisQueryRequestDto.GroupBy),
                $"GroupBy column '{colName}' does not exist in this dataset version.");
            return null;
        }

        return column;
    }

    private List<ValidatedAggregate> ValidateAggregates(
        IReadOnlyList<AggregateRequestDto> aggregates,
        Dictionary<string, DatasetColumn> columnMap,
        Dictionary<string, List<string>> errors)
    {
        var result = new List<ValidatedAggregate>(aggregates.Count);

        for (var i = 0; i < aggregates.Count; i++)
        {
            var agg = aggregates[i];
            var prefix = $"Aggregates[{i}]";

            if (string.IsNullOrWhiteSpace(agg.Column))
            {
                if (agg.Function != AggregateFunction.Count)
                {
                    AddError(errors, $"{prefix}.Column",
                        $"Column is required for aggregate function '{agg.Function}'.");
                    continue;
                }

                result.Add(new ValidatedAggregate(agg.GetOutputName(), AggregateFunction.Count, null, null));
                continue;
            }

            var colName = agg.Column.Trim();
            if (!columnMap.TryGetValue(colName, out var column))
            {
                AddError(errors, $"{prefix}.Column",
                    $"Aggregate column '{colName}' does not exist in this dataset version.");
                continue;
            }

            var isAllowed = agg.Function switch
            {
                AggregateFunction.Count => true,
                AggregateFunction.Sum or AggregateFunction.Avg => _converter.IsNumericType(column.DataType),
                AggregateFunction.Min or AggregateFunction.Max =>
                    _converter.IsNumericType(column.DataType) || _converter.IsDateTimeType(column.DataType),
                _ => false
            };

            if (!isAllowed)
            {
                AddError(errors, $"{prefix}.Function",
                    $"Aggregate function '{agg.Function}' is not supported for column '{column.Name}' of type '{column.DataType}'.");
                continue;
            }

            result.Add(new ValidatedAggregate(agg.GetOutputName(), agg.Function, column.Name, column.DataType));
        }

        return result;
    }

    private bool IsOperatorCompatible(FilterOperator op, string dataType)
    {
        return op switch
        {
            FilterOperator.IsNull or FilterOperator.IsNotNull or FilterOperator.Eq or FilterOperator.Neq => true,
            FilterOperator.Contains or FilterOperator.StartsWith or FilterOperator.EndsWith =>
                _converter.IsStringType(dataType),
            FilterOperator.Gt or FilterOperator.Gte or FilterOperator.Lt or FilterOperator.Lte =>
                _converter.IsNumericType(dataType) || _converter.IsDateTimeType(dataType),
            _ => false
        };
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }

        list.Add(message);
    }

    private static void ThrowIfHasErrors(Dictionary<string, List<string>> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));
        }
    }
}