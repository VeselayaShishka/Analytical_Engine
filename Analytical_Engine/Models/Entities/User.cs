namespace Analytical_Engine.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Analyst;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}