using System.Text.Json;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Analysis;
using Xunit;

namespace Analytical_Engine.Tests;

public class AnalysisModuleTests
{
    private readonly ColumnValueConverter _converter = new();
    private readonly AnalysisRequestValidator _validator;
    private readonly AnalysisEngine _engine;

    public AnalysisModuleTests()
    {
        _validator = new AnalysisRequestValidator(_converter);
        _engine = new AnalysisEngine(_converter);
    }

    private static IReadOnlyList<DatasetColumn> CreateSampleColumns() =>
    [
        new DatasetColumn { Id = Guid.NewGuid(), Name = "category", DataType = "string", IsNullable = true, Position = 0 },
        new DatasetColumn { Id = Guid.NewGuid(), Name = "price", DataType = "decimal", IsNullable = true, Position = 1 },
        new DatasetColumn { Id = Guid.NewGuid(), Name = "quantity", DataType = "int", IsNullable = false, Position = 2 },
        new DatasetColumn { Id = Guid.NewGuid(), Name = "created_at", DataType = "datetime", IsNullable = false, Position = 3 }
    ];

    private static async IAsyncEnumerable<string> ToAsyncStream(IEnumerable<string> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
            await Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData("42", "int", true)]
    [InlineData("abc", "int", false)]
    [InlineData("125.50", "decimal", true)]
    [InlineData("125,50", "decimal", true)]
    [InlineData("2026-11-20T10:00:00Z", "datetime", true)]
    public void ColumnValueConverter_ShouldRespectInvariantCultureRules(string raw, string type, bool expectedValid)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(raw));
        var isValid = _converter.TryConvert(doc.RootElement, type, out var result);

        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void Validator_ShouldCollectAllSemanticErrorsAtOnce()
    {
        var request = new AnalysisQueryRequestDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Filters =
            [
                new FilterConditionDto { Column = "unknown_col", Operator = FilterOperator.Eq, Value = JsonDocument.Parse("1").RootElement },
                new FilterConditionDto { Column = "price", Operator = FilterOperator.Contains, Value = JsonDocument.Parse("\"10\"").RootElement },
                new FilterConditionDto { Column = "quantity", Operator = FilterOperator.Gt, Value = JsonDocument.Parse("\"abc\"").RootElement }
            ],
            GroupBy = "missing_group",
            Aggregates =
            [
                new AggregateRequestDto { Function = AggregateFunction.Sum, Column = "category" }
            ]
        };

        var ex = Assert.Throws<RequestValidationException>(() =>
            _validator.ValidateQuery(request, CreateSampleColumns()));

        Assert.Equal(5, ex.Errors.Count);
    }

    [Fact]
    public async Task Engine_GroupByWithNullAndMultipleAggregates_ShouldComputeAndSortProperly()
    {
        var rows = new[]
        {
            """{"category":"Books","price":"100.00","quantity":"2"}""",
            """{"category":"Books","price":"200.00","quantity":"3"}""",
            """{"category":"Audio","price":"50.00","quantity":"1"}""",
            """{"category":null,"price":"300.00","quantity":"4"}"""
        };

        var request = new AnalysisQueryRequestDto
        {
            DatasetVersionId = Guid.NewGuid(),
            GroupBy = "category",
            Aggregates =
            [
                new AggregateRequestDto { Function = AggregateFunction.Sum, Column = "price" },
                new AggregateRequestDto { Function = AggregateFunction.Avg, Column = "price" },
                new AggregateRequestDto { Function = AggregateFunction.Count }
            ]
        };

        var plan = _validator.ValidateQuery(request, CreateSampleColumns());
        var output = await _engine.ExecuteQueryAsync(ToAsyncStream(rows), plan);

        Assert.Equal(4, output.RecordsProcessed);
        Assert.Equal(3, output.Groups.Count);

        // Null-группа идёт первой, затем Audio, затем Books
        Assert.Null(output.Groups[0].Key);
        Assert.Equal(300.00m, output.Groups[0].Values["sum(price)"]);

        Assert.Equal("Audio", output.Groups[1].Key);
        Assert.Equal(50.00m, output.Groups[1].Values["avg(price)"]);

        Assert.Equal("Books", output.Groups[2].Key);
        Assert.Equal(300.00m, output.Groups[2].Values["sum(price)"]);
        Assert.Equal(150.00m, output.Groups[2].Values["avg(price)"]);
        Assert.Equal(2L, output.Groups[2].Count);
    }

    [Fact]
    public async Task Engine_Filters_ShouldExcludeNullsExceptIsNull()
    {
        var rows = new[]
        {
            """{"category":"Books","price":null,"quantity":"1"}""",
            """{"category":"Books","price":"150.00","quantity":"2"}""",
            """{"category":"Books","price":"80.00","quantity":"3"}"""
        };

        var request = new AnalysisQueryRequestDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Filters =
            [
                new FilterConditionDto { Column = "price", Operator = FilterOperator.Gt, Value = JsonDocument.Parse("100").RootElement }
            ],
            Aggregates = [new AggregateRequestDto { Function = AggregateFunction.Count }]
        };

        var plan = _validator.ValidateQuery(request, CreateSampleColumns());
        var output = await _engine.ExecuteQueryAsync(ToAsyncStream(rows), plan);

        Assert.Equal(1, output.RecordsProcessed);
        Assert.Equal(1L, output.Groups[0].Values["count(*)"]);
    }

    [Fact]
    public async Task Engine_Statistics_SingleRecord_ShouldReturnNullStdDevAndVariance()
    {
        var rows = new[]
        {
            """{"category":"Books","price":"42.50","quantity":"1"}""",
            """{"category":"Books","price":null,"quantity":"1"}"""
        };

        var request = new StatisticsRequestDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Column = "price"
        };

        var plan = _validator.ValidateStatistics(request, CreateSampleColumns());
        var output = await _engine.ExecuteStatisticsAsync(ToAsyncStream(rows), plan);

        var stats = Assert.Single(output.Statistics);
        Assert.Equal(1, stats.Count);
        Assert.Equal(1, stats.NullCount);
        Assert.Equal(42.50m, stats.Sum);
        Assert.Equal(42.5, stats.Mean);
        Assert.Equal(42.5, stats.Median);
        Assert.Null(stats.Variance);
        Assert.Null(stats.StdDev);
    }

    [Fact]
    public async Task Engine_Statistics_MultipleRecords_ShouldComputeWelfordStdDevAndQuartiles()
    {
        var rows = new[]
        {
            """{"price":"10"}""",
            """{"price":"20"}""",
            """{"price":"30"}""",
            """{"price":"40"}"""
        };

        var request = new StatisticsRequestDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Column = "price"
        };

        var plan = _validator.ValidateStatistics(request, CreateSampleColumns());
        var output = await _engine.ExecuteStatisticsAsync(ToAsyncStream(rows), plan);

        var stats = Assert.Single(output.Statistics);
        Assert.Equal(4, stats.Count);
        Assert.Equal(100m, stats.Sum);
        Assert.Equal(25.0, stats.Mean);
        Assert.Equal(25.0, stats.Median);
        Assert.Equal(17.5, stats.Q1);
        Assert.Equal(32.5, stats.Q3);
        Assert.Equal(15.0, stats.Iqr);
        // Выборочная дисперсия: ((15^2 + 5^2 + 5^2 + 15^2) / 3) = 500 / 3 = 166.6667
        Assert.NotNull(stats.Variance);
        Assert.Equal(500.0 / 3.0, stats.Variance!.Value, precision: 6);
        Assert.Equal(Math.Sqrt(500.0 / 3.0), stats.StdDev!.Value, precision: 6);
    }
}