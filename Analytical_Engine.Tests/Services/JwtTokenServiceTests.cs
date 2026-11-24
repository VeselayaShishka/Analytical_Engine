using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Analytical_Engine.Models;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services;
using Analytical_Engine.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings = new()
    {
        Secret = "SuperSecretKeyForTestingJwtTokenServiceAtLeast32Chars!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 45
    };

    [Fact]
    public void GenerateToken_ValidUser_ReturnsTokenWithClaimsAndExpiration()
    {
        var options = Options.Create(_jwtSettings);
        var service = new JwtTokenService(options);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testanalyst",
            Email = "analyst@example.com",
            Role = Roles.Analyst
        };

        var beforeGeneration = DateTime.UtcNow;
        var tokenResult = service.GenerateToken(user);
        var afterGeneration = DateTime.UtcNow;

        Assert.NotNull(tokenResult);
        Assert.False(string.IsNullOrWhiteSpace(tokenResult.Token));

        var expectedMinExpiration = beforeGeneration.AddMinutes(44);
        var expectedMaxExpiration = afterGeneration.AddMinutes(46);
        Assert.InRange(tokenResult.Expiration, expectedMinExpiration, expectedMaxExpiration);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.Token);

        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);

        var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(idClaim);
        Assert.Equal(user.Id.ToString(), idClaim.Value);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal(user.Username, nameClaim.Value);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(user.Role, roleClaim.Value);
    }
}
