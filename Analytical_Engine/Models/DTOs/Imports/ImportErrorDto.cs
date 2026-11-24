namespace Analytical_Engine.Models.DTOs.Imports;

public record ImportErrorDto
{
    public int RowNumber { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}
