using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Services.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(User user );
}

public record JwtTokenResult(string Token, DateTime Expiration);