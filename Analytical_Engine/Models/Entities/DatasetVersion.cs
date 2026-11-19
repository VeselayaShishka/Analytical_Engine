using System.Runtime.InteropServices.JavaScript;

namespace Analytical_Engine.Models.Entities;

public class DatasetVersion
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public int Version { get; set; }
    public string Format { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long RecordCount { get; set; }
    public DateTime Created { get; set; }
    
    public Dataset Dataset { get; set; } = null!;
    public ICollection<DatasetColumn> Columns { get; set; } = new List<DatasetColumn>();
    public ICollection<DataRecord> Records { get; set; } = new List<DataRecord>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}