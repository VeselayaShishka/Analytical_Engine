using System.Text.Json;

namespace Analytical_Engine.Models.DTOs.Datasets;

public class DataRecordResponseDto
{
    public Guid Id { get; set; }
    public long RowNumber { get; set; }
    public JsonElement Data { get; set; }
}
