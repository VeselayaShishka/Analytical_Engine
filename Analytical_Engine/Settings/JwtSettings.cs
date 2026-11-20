using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JwtSettings:Secret is required.")]
    [MinLength(32, ErrorMessage = "JwtSettings:Secret must be at least 32 characters long for HMAC-SHA256.")]
    public string Secret { get; set; } = string.Empty;

    [Required(ErrorMessage = "JwtSettings:Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JwtSettings:Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 43200, ErrorMessage = "JwtSettings:ExpirationMinutes must be between 1 and 43200.")]
    public int ExpirationMinutes { get; set; } = 60;
}