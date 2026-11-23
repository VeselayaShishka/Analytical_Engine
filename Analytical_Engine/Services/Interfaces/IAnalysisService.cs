using Analytical_Engine.Models.DTOs.Analysis;

namespace Analytical_Engine.Services.Interfaces;

public interface IAnalysisService
{
    Task<AnalysisResultDto<IReadOnlyList<GroupResultItem>>> ExecuteAsync(
        AnalysisQueryRequestDto request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<AnalysisResultDto<IReadOnlyList<StatisticsResult>>> GetStatisticsAsync(
        StatisticsRequestDto request,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}