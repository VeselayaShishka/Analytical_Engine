using System.Security.Claims;
using Analytical_Engine.Controllers;
using Analytical_Engine.Models;
using Analytical_Engine.Models.DTOs.Auth;
using Analytical_Engine.Models.DTOs.Users;
using Analytical_Engine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Analytical_Engine.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_authServiceMock.Object);
    }

    private void SetUserContext(Guid userId, string role = Roles.Analyst)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Register_ValidDto_ReturnsCreatedResult()
    {
        var dto = new UserCreateDto { Username = "alice", Email = "alice@example.com", Password = "Pass" };
        var createdUser = new UserResponseDto { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com", Role = Roles.Analyst };

        _authServiceMock.Setup(s => s.RegisterAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult<UserResponseDto>.Success(createdUser));

        var actionResult = await _controller.Register(dto, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal(createdUser, objectResult.Value);
    }

    [Fact]
    public async Task Register_Conflict_ReturnsConflictProblem()
    {
        var dto = new UserCreateDto { Username = "alice", Email = "alice@example.com", Password = "Pass" };
        _authServiceMock.Setup(s => s.RegisterAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult<UserResponseDto>.Conflict("User already exists"));

        var actionResult = await _controller.Register(dto, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResult()
    {
        var dto = new LoginDto { Email = "alice@example.com", Password = "Pass" };
        var response = new AuthResponseDto
        {
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserResponseDto { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com" }
        };

        _authServiceMock.Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult<AuthResponseDto>.Success(response));

        var actionResult = await _controller.Login(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "alice@example.com", Password = "Wrong" };
        _authServiceMock.Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult<AuthResponseDto>.Unauthorized("Invalid credentials."));

        var actionResult = await _controller.Login(dto, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsOkResult()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId);

        var user = new UserResponseDto { Id = userId, Username = "testuser", Email = "test@example.com" };
        _authServiceMock.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult<UserResponseDto>.Success(user));

        var actionResult = await _controller.GetMe(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(user, okResult.Value);
    }

    [Fact]
    public async Task GetMe_MissingUserIdentifier_ReturnsUnauthorizedProblem()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var actionResult = await _controller.GetMe(CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }
}
