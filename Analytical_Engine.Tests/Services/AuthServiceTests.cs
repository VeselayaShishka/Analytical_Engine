using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Auth;
using Analytical_Engine.Models.DTOs.Users;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Interfaces;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _authService = new AuthService(_uowMock.Object, _hasherMock.Object, _jwtMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidDto_SuccessfullyRegistersAndReturnsUser()
    {
        var dto = new UserCreateDto
        {
            Username = "newanalyst",
            Email = "newanalyst@example.com",
            Password = "Password123!"
        };

        _userRepoMock.Setup(r => r.EmailExistsAsync("newanalyst@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepoMock.Setup(r => r.UsernameExistsAsync("newanalyst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hasherMock.Setup(h => h.Hash("Password123!")).Returns("HashedPassword123");

        var result = await _authService.RegisterAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("newanalyst", result.Data.Username);
        Assert.Equal("newanalyst@example.com", result.Data.Email);
        Assert.Equal(Roles.Analyst, result.Data.Role);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "newanalyst" &&
            u.Email == "newanalyst@example.com" &&
            u.PasswordHash == "HashedPassword123" &&
            u.Role == Roles.Analyst), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsConflict()
    {
        var dto = new UserCreateDto
        {
            Username = "user1",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _userRepoMock.Setup(r => r.EmailExistsAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _authService.RegisterAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Conflict, result.ErrorType);
        Assert.Contains("email already exists", result.ErrorMessage);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyExists_ReturnsConflict()
    {
        var dto = new UserCreateDto
        {
            Username = "existinguser",
            Email = "new@example.com",
            Password = "Password123!"
        };

        _userRepoMock.Setup(r => r.EmailExistsAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepoMock.Setup(r => r.UsernameExistsAsync("existinguser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _authService.RegisterAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Conflict, result.ErrorType);
        Assert.Contains("username already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        var dto = new LoginDto
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "validuser",
            PasswordHash = "StoredHash",
            IsActive = true,
            Role = Roles.Analyst
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("ValidPassword123!", "StoredHash"))
            .Returns(true);

        var expectedExpiry = DateTime.UtcNow.AddHours(2);
        _jwtMock.Setup(j => j.GenerateToken(user))
            .Returns(new JwtTokenResult("GeneratedJwtToken", expectedExpiry));

        var result = await _authService.LoginAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("GeneratedJwtToken", result.Data.Token);
        Assert.Equal(expectedExpiry, result.Data.ExpiresAt);
        Assert.Equal(user.Id, result.Data.User.Id);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "notfound@example.com", Password = "Pass" };
        _userRepoMock.Setup(r => r.GetByEmailAsync("notfound@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "inactive@example.com", Password = "Pass" };
        var user = new User { Email = "inactive@example.com", IsActive = false };
        _userRepoMock.Setup(r => r.GetByEmailAsync("inactive@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "WrongPassword" };
        var user = new User { Email = "user@example.com", PasswordHash = "CorrectHash", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("WrongPassword", "CorrectHash")).Returns(false);

        var result = await _authService.LoginAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ExistingActiveUser_ReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "activeuser", Email = "active@example.com", IsActive = true };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _authService.GetCurrentUserAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(userId, result.Data.Id);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNotFoundOrInactive_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _authService.GetCurrentUserAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorType.Unauthorized, result.ErrorType);
    }
}
