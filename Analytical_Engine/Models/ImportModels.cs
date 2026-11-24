using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Models;

public record Metadata(Guid DatasetId,
    string SourceName,
    string Email,
    string Format,
    DateTime CreatedAt);

public record SchemaField(
    string Type,
    bool IsNullable = false,
    String? Description = null
);

public record Schema(
    Dictionary<string, SchemaField> Fields
);

public record RecordData(int RowNumber, Dictionary<string, object?> Content);

public record ImportError(int RowNumber, List<string> ErrorMessages);

public record ImportResult(bool IsSuccess, string Message, DatasetVersion? Version = null, List<ImportError> Errors = null);