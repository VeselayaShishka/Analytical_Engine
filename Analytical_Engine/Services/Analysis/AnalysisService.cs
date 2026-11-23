using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Extensions;
using Analytical_Engine.Models.DTOs.Analysis;
using Analytical_Engine.Services.Analysis;
using Analytical_Engine.Services.Interfaces;

namespace Analytical_Engine.Services.Analysis;

public class AnalysisService : IAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AnalysisRequestValidator _validator;
    private readonly AnalysisEngine _engine;

    public AnalysisService(
        IUnitOfWork unitOfWork,
        AnalysisRequestValidator validator,
        AnalysisEngine engine)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _engine = engine;
    }

    public async Task<AnalysisResultDto<IReadOnlyList<GroupResultItem>>> ExecuteAsync(
        AnalysisQueryRequestDto request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var version = await DatasetAccessHelper.GetAccessibleVersionWithColumnsAsync(
            _unitOfWork,
            request.DatasetVersionId,
            userId,
            isAdmin,
            cancellationToken);

        var plan = _validator.ValidateQuery(request, version.Columns);

        var rowsStream = _unitOfWork.DataRecords
            .StreamDataByVersionIdAsync(version.Id, cancellationToken);

        var output = await _engine.ExecuteQueryAsync(rowsStream, plan, cancellationToken);

        var aggregatesDescription = string.Join(", ", plan.Aggregates.Select(a => a.OutputName));
        var operation = plan.GroupByColumn is null
            ? aggregatesDescription
            : $"{aggregatesDescription} group by {plan.GroupByColumn.Name}";

        return new AnalysisResultDto<IReadOnlyList<GroupResultItem>>
        {
            DatasetVersionId = version.Id,
            Operation = operation,
            RecordsProcessed = output.RecordsProcessed,
            Result = output.Groups
        };
    }

    public async Task<AnalysisResultDto<IReadOnlyList<StatisticsResult>>> GetStatisticsAsync(
        StatisticsRequestDto request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var version = await DatasetAccessHelper.GetAccessibleVersionWithColumnsAsync(
            _unitOfWork,
            request.DatasetVersionId,
            userId,
            isAdmin,
            cancellationToken);

        var plan = _validator.ValidateStatistics(request, version.Columns);

        var rowsStream = _unitOfWork.DataRecords
            .StreamDataByVersionIdAsync(version.Id, cancellationToken);

        var output = await _engine.ExecuteStatisticsAsync(rowsStream, plan, cancellationToken);

        var operation = plan.GroupByColumn is null
            ? $"statistics({plan.TargetColumn.Name})"
            : $"statistics({plan.TargetColumn.Name}) group by {plan.GroupByColumn.Name}";

        return new AnalysisResultDto<IReadOnlyList<StatisticsResult>>
        {
            DatasetVersionId = version.Id,
            Operation = operation,
            RecordsProcessed = output.RecordsProcessed,
            Result = output.Statistics
        };
    }
}