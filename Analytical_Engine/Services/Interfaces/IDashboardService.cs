using Analytical_Engine.Models.DTOs.Dashboard;

namespace Analytical_Engine.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}