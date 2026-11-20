namespace Analytical_Engine.Settings;

public class SeedAdminSettings
{
    public const string SectionName = "SeedAdmin";

    public string Username { get; set; } = "admin";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}