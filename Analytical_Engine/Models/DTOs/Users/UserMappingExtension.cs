using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Models.DTOs.Users;

public static class UserMappingExtension
{
    public static UserResponseDto ToResponseDto(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}