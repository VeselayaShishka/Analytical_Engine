using System.Globalization;
using System.Text.Json;
using Analytical_Engine.Models.DTOs.Analysis;

namespace Analytical_Engine.Services.Analysis;

public interface IAggregateAccumulator
{
    void Accumulate(JsonElement? cellValue);
    object? GetResult();
}

public sealed class CountAccumulator : IAggregateAccumulator
{
    private readonly bool _countAllRows;
    private long _count;

    public CountAccumulator(bool countAllRows)
    {
        _countAllRows = countAllRows;
    }

    public void Accumulate(JsonElement? cellValue)
    {
        if (_countAllRows)
        {
            _count++;
            return;
        }

        if (cellValue.HasValue &&
            cellValue.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
        {
            _count++;
        }
    }

    public object? GetResult() => _count;
}

public sealed class SumAccumulator : IAggregateAccumulator
{
    private readonly string _dataType;
    private readonly ColumnValueConverter _converter;
    private readonly bool _isAverage;
    private long _count;
    private decimal _decimalSum;
    private double _doubleSum;

    public SumAccumulator(string dataType, ColumnValueConverter converter, bool isAverage)
    {
        _dataType = dataType;
        _converter = converter;
        _isAverage = isAverage;
    }

    public void Accumulate(JsonElement? cellValue)
    {
        if (!cellValue.HasValue ||
            cellValue.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (!_converter.TryConvertToNumeric(cellValue.Value, _dataType, out var numeric))
        {
            return;
        }

        _count++;
        if (numeric.IsDouble)
        {
            _doubleSum += numeric.DoubleValue;
        }
        else
        {
            _decimalSum += numeric.DecimalValue;
        }
    }

    public object? GetResult()
    {
        if (_count == 0)
        {
            return null;
        }

        if (_converter.IsExactDecimalType(_dataType))
        {
            return _isAverage ? _decimalSum / _count : _decimalSum;
        }

        return _isAverage ? _doubleSum / _count : _doubleSum;
    }
}

public sealed class MinMaxAccumulator : IAggregateAccumulator
{
    private readonly string _dataType;
    private readonly ColumnValueConverter _converter;
    private readonly bool _isMax;
    private IComparable? _best;

    public MinMaxAccumulator(string dataType, ColumnValueConverter converter, bool isMax)
    {
        _dataType = dataType;
        _converter = converter;
        _isMax = isMax;
    }

    public void Accumulate(JsonElement? cellValue)
    {
        if (!cellValue.HasValue ||
            cellValue.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (!_converter.TryConvert(cellValue.Value, _dataType, out var converted) || converted is null)
        {
            return;
        }

        IComparable candidate = converted switch
        {
            int i => (decimal)i,
            long l => (decimal)l,
            decimal d => d,
            double dbl => dbl,
            DateTime dt => dt,
            _ => (IComparable)converted
        };

        if (_best is null)
        {
            _best = candidate;
            return;
        }

        var cmp = candidate.CompareTo(_best);
        if ((_isMax && cmp > 0) || (!_isMax && cmp < 0))
        {
            _best = candidate;
        }
    }

    public object? GetResult() => _best;
}

public sealed class GroupAccumulatorSet
{
    private readonly IReadOnlyList<ValidatedAggregate> _specs;
    private readonly IAggregateAccumulator[] _accumulators;

    public long TotalCount { get; private set; }

    public GroupAccumulatorSet(IReadOnlyList<ValidatedAggregate> specs, ColumnValueConverter converter)
    {
        _specs = specs;
        _accumulators = new IAggregateAccumulator[specs.Count];

        for (var i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            _accumulators[i] = spec.Function switch
            {
                AggregateFunction.Count => new CountAccumulator(spec.ColumnName is null),
                AggregateFunction.Sum => new SumAccumulator(spec.DataType!, converter, isAverage: false),
                AggregateFunction.Avg => new SumAccumulator(spec.DataType!, converter, isAverage: true),
                AggregateFunction.Min => new MinMaxAccumulator(spec.DataType!, converter, isMax: false),
                AggregateFunction.Max => new MinMaxAccumulator(spec.DataType!, converter, isMax: true),
                _ => throw new NotSupportedException($"Unsupported aggregate function: {spec.Function}")
            };
        }
    }

    public void AccumulateRow(JsonElement root)
    {
        TotalCount++;

        for (var i = 0; i < _specs.Count; i++)
        {
            var colName = _specs[i].ColumnName;
            JsonElement? cell = colName is null ? null : JsonRowHelper.GetPropertyCaseInsensitive(root, colName);
            _accumulators[i].Accumulate(cell);
        }
    }

    public Dictionary<string, object?> BuildValues()
    {
        var result = new Dictionary<string, object?>(_specs.Count, StringComparer.Ordinal);
        for (var i = 0; i < _specs.Count; i++)
        {
            result[_specs[i].OutputName] = _accumulators[i].GetResult();
        }
        return result;
    }
}

public sealed class WelfordStatisticsAccumulator
{
    private readonly string _dataType;
    private readonly ColumnValueConverter _converter;
    private readonly List<double> _values = new();

    private long _count;
    private long _nullCount;
    private decimal _decimalSum;
    private double _doubleSum;
    private double _mean;
    private double _m2;
    private decimal? _minDecimal;
    private decimal? _maxDecimal;
    private double? _minDouble;
    private double? _maxDouble;

    public WelfordStatisticsAccumulator(string dataType, ColumnValueConverter converter)
    {
        _dataType = dataType;
        _converter = converter;
    }

    public void Accumulate(JsonElement? cellValue)
    {
        if (!cellValue.HasValue ||
            cellValue.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            _nullCount++;
            return;
        }

        if (!_converter.TryConvertToNumeric(cellValue.Value, _dataType, out var numeric))
        {
            _nullCount++;
            return;
        }

        _count++;
        var x = numeric.AsDouble();
        _values.Add(x);

        var delta = x - _mean;
        _mean += delta / _count;
        var delta2 = x - _mean;
        _m2 += delta * delta2;

        if (numeric.IsDouble)
        {
            _doubleSum += numeric.DoubleValue;
            _minDouble = _minDouble.HasValue ? Math.Min(_minDouble.Value, numeric.DoubleValue) : numeric.DoubleValue;
            _maxDouble = _maxDouble.HasValue ? Math.Max(_maxDouble.Value, numeric.DoubleValue) : numeric.DoubleValue;
        }
        else
        {
            _decimalSum += numeric.DecimalValue;
            _minDecimal = _minDecimal.HasValue ? Math.Min(_minDecimal.Value, numeric.DecimalValue) : numeric.DecimalValue;
            _maxDecimal = _maxDecimal.HasValue ? Math.Max(_maxDecimal.Value, numeric.DecimalValue) : numeric.DecimalValue;
        }
    }

    public StatisticsResult BuildResult(string? groupKey)
    {
        if (_count == 0)
        {
            return new StatisticsResult
            {
                Key = groupKey,
                Count = 0,
                NullCount = _nullCount
            };
        }

        _values.Sort();

        var q1 = ComputeQuantile(_values, 0.25);
        var median = ComputeQuantile(_values, 0.50);
        var q3 = ComputeQuantile(_values, 0.75);

        double? variance = _count >= 2 ? _m2 / (_count - 1) : null;
        double? stdDev = variance.HasValue ? Math.Sqrt(variance.Value) : null;

        var isExact = _converter.IsExactDecimalType(_dataType);

        return new StatisticsResult
        {
            Key = groupKey,
            Count = _count,
            NullCount = _nullCount,
            Sum = isExact ? _decimalSum : _doubleSum,
            Mean = _mean,
            Min = isExact ? _minDecimal : _minDouble,
            Max = isExact ? _maxDecimal : _maxDouble,
            Median = median,
            Variance = variance,
            StdDev = stdDev,
            Q1 = q1,
            Q3 = q3,
            Iqr = q3 - q1
        };
    }

    private static double ComputeQuantile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var position = percentile * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var fraction = position - lowerIndex;
        return sortedValues[lowerIndex] + fraction * (sortedValues[upperIndex] - sortedValues[lowerIndex]);
    }
}

internal static class JsonRowHelper
{
    public static JsonElement? GetPropertyCaseInsensitive(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (root.TryGetProperty(propertyName, out var exact))
        {
            return exact;
        }

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value;
            }
        }

        return null;
    }

    public static string? FormatGroupKey(object? value)
    {
        return value switch
        {
            null => null,
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}