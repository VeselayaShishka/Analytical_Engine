namespace Analytical_Engine.Models.Entities;

public class ErrorLog
{
    public Guid Id { get; set; }
    public Guid? DatasetId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
