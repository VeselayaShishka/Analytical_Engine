using System.Security.Claims;
using System.Text;
using Analytical_Engine.Data;
using Analytical_Engine.Data.Repositories;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Services;
using Analytical_Engine.Services.Analysis;
using Analytical_Engine.Services.Interfaces;
using Analytical_Engine.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<SeedAdminSettings>(
    builder.Configuration.GetSection(SeedAdminSettings.SectionName));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAnalyst", policy =>
        policy.RequireRole(Roles.Analyst, Roles.DataAdministrator));

    options.AddPolicy("RequireDataAdmin", policy =>
        policy.RequireRole(Roles.DataAdministrator));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddTransient<CSVImportService>();
builder.Services.AddTransient<JSONImportService>();

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<Analytical_Engine.Exceptions.GlobalExceptionHandler>();

builder.Services.AddSingleton<ColumnValueConverter>();
builder.Services.AddSingleton<AnalysisRequestValidator>();
builder.Services.AddSingleton<AnalysisEngine>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();

var app = builder.Build();

await AdminSeeder.SeedAsync(app.Services);

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();