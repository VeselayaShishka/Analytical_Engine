using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services.Interfaces;
using Analytical_Engine.Settings;
using Microsoft.Extensions.Options;

namespace Analytical_Engine.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var settings = scope.ServiceProvider
            .GetRequiredService<IOptions<SeedAdminSettings>>().Value;

        if (string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        if (settings.Password.Length < 15)
        {
            throw new InvalidOperationException(
                "SeedAdmin:Password must be at least 15 characters long.");
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var normalizedEmail = settings.Email.Trim().ToLowerInvariant();
        var existingUser = await unitOfWork.Users.GetByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = string.IsNullOrWhiteSpace(settings.Username)
                ? "admin"
                : settings.Username.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(settings.Password),
            Role = Roles.DataAdministrator,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await unitOfWork.Users.AddAsync(admin);
        await unitOfWork.SaveChangesAsync();
    }
}