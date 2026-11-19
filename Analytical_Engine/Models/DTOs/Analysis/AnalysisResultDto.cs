namespace Analytical_Engine.Models.DTOs.Analysis;

public class AnalysisResultDto
{
    public string Operation { get; set; } = string.Empty;
    public long RecordsProcessed { get; set; }
    public object? Result { get; set; }
}
