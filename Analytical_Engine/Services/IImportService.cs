using Analytical_Engine.Models;

namespace Analytical_Engine.Services;

public interface IImportService
{
    Task<ImportResult> ImportDataset(Stream fileStream, Schema schema, Metadata metadata);
}