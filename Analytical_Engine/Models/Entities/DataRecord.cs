namespace Analytical_Engine.Models.Entities;

public class DataRecord
{
    public Guid Id { get; set; }
    public Guid DatasetVersionId { get; set; }
    public long RowNumber { get; set; }
    public string Data { get; set; } = "{}";

    public DatasetVersion DatasetVersion { get; set; } = null!;
}