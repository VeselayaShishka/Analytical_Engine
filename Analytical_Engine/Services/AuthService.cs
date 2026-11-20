using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Auth;
using Analytical_Engine.Models.DTOs.Users;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services.Interfaces;

namespace Analytical_Engine.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult<UserResponseDto>> RegisterAsync(
        UserCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var normalizedUsername = dto.Username.Trim();

        if (await _unitOfWork.Users.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return AuthResult<UserResponseDto>.Conflict("User with this email already exists.");
        }

        if (await _unitOfWork.Users.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return AuthResult<UserResponseDto>.Conflict("User with this username already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = Roles.Analyst,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult<UserResponseDto>.Success(user.ToResponseDto());
    }

    public async Task<AuthResult<AuthResponseDto>> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            return AuthResult<AuthResponseDto>.Unauthorized("Invalid credentials.");
        }

        var tokenResult = _jwtTokenService.GenerateToken(user);

        var response = new AuthResponseDto
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.Expiration,
            User = user.ToResponseDto()
        };

        return AuthResult<AuthResponseDto>.Success(response);
    }

    public async Task<AuthResult<UserResponseDto>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return AuthResult<UserResponseDto>.Unauthorized("User account not found or inactive.");
        }

        return AuthResult<UserResponseDto>.Success(user.ToResponseDto());
    }
}