using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Extensions;

public static class DatasetAccessHelper
{
    public static async Task<DatasetVersion> GetAccessibleVersionWithColumnsAsync(
        IUnitOfWork unitOfWork,
        Guid versionId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var version = await unitOfWork.DatasetVersions
            .GetWithColumnsAsync(versionId, cancellationToken);

        if (version is null)
        {
            throw new NotFoundException($"Dataset version '{versionId}' was not found.");
        }

        var dataset = await unitOfWork.Datasets
            .GetDtoByIdAsync(version.DatasetId, cancellationToken);

        if (dataset is null || (!isAdmin && dataset.UserId != userId))
        {
            throw new NotFoundException($"Dataset version '{versionId}' was not found.");
        }

        return version;
    }
}