using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Datasets;

public class DatasetCreateDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}
