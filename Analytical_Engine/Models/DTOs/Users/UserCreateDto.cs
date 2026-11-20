using System.ComponentModel.DataAnnotations;

namespace Analytical_Engine.Models.DTOs.Users;

public class UserCreateDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(15, ErrorMessage = "Password must be at least 15 characters long.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;
}
