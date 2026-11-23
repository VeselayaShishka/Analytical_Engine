using System.ComponentModel.DataAnnotations;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Models.DTOs.Reports;
using Xunit;

namespace Analytical_Engine.Tests;

public class ReportsValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ReportCreateDto_MissingAnalysisRequest_ShouldFailValidation()
    {
        var dto = new ReportCreateDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Name = "Empty Report",
            ReportType = ReportType.Aggregation
        };

        var errors = ValidateModel(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ReportCreateDto_GroupingWithoutGroupBy_ShouldFailValidation()
    {
        var dto = new ReportCreateDto
        {
            DatasetVersionId = Guid.NewGuid(),
            Name = "Invalid Grouping Report",
            ReportType = ReportType.Grouping,
            QueryRequest = new AnalysisQueryRequestDto
            {
                DatasetVersionId = Guid.NewGuid(),
                GroupBy = null,
                Aggregates = [new AggregateRequestDto { Function = AggregateFunction.Count }]
            }
        };

        var errors = ValidateModel(dto);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("requires GroupBy"));
    }

    [Fact]
    public void ReportCreateDto_ValidGroupingRequest_ShouldPassValidation()
    {
        var versionId = Guid.NewGuid();
        var dto = new ReportCreateDto
        {
            DatasetVersionId = versionId,
            Name = "Sales by Category",
            ReportType = ReportType.Grouping,
            Description = "Total revenue breakdown across product categories.",
            QueryRequest = new AnalysisQueryRequestDto
            {
                DatasetVersionId = versionId,
                GroupBy = "category",
                Aggregates = [new AggregateRequestDto { Function = AggregateFunction.Sum, Column = "price" }]
            }
        };

        var errors = ValidateModel(dto);
        Assert.Empty(errors);
    }
}