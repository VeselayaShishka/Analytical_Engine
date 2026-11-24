using Analytical_Engine.Services;
using Xunit;

namespace Analytical_Engine.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ValidPassword_ReturnsNonEmptyHash()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.Hash(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_NullOrWhitespace_ThrowsArgumentException(string password)
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(password));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        var password = "CorrectPassword123!";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify("WrongPassword456!", hash);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("password", "")]
    [InlineData("  ", "hash")]
    [InlineData("password", "   ")]
    public void Verify_EmptyInputs_ReturnsFalse(string password, string hash)
    {
        var isValid = _hasher.Verify(password, hash);

        Assert.False(isValid);
    }
}
