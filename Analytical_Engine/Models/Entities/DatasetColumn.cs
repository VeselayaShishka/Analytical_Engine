namespace Analytical_Engine.Models.Entities;

public class DatasetColumn
{
    public Guid Id { get; set; }
    public Guid DatasetVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public int Position { get; set; }
    public string? Description { get; set; }

    public DatasetVersion DatasetVersion { get; set; } = null!;
}