using Analytical_Engine.Models.DTOs.Auth;
using Analytical_Engine.Models.DTOs.Users;

namespace Analytical_Engine.Services.Interfaces;

public enum AuthErrorType
{
    None,
    Conflict,
    Unauthorized
}

public record AuthResult<T>(bool IsSuccess, T? Data, AuthErrorType ErrorType = AuthErrorType.None, string? ErrorMessage = null)
{
    public static AuthResult<T> Success(T data) => new(true, data);
    public static AuthResult<T> Conflict(string message) => new(false, default, AuthErrorType.Conflict, message);
    public static AuthResult<T> Unauthorized(string message) => new(false, default, AuthErrorType.Unauthorized, message);
}

public interface IAuthService
{
    Task<AuthResult<UserResponseDto>> RegisterAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<AuthResult<UserResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}