using Analytical_Engine.Models.DTOs.Users;

namespace Analytical_Engine.Models.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponseDto User { get; set; } = null!;
}