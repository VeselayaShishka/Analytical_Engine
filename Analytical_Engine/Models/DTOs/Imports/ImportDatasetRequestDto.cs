using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Analytical_Engine.Models.DTOs.Imports;

public class ImportDatasetRequestDto
{
    [Required]
    public Guid DatasetId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;

    public string? SchemaJson { get; set; }
}
