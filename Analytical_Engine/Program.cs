using Analytical_Engine.Data;
using Analytical_Engine.Data.Repositories;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddTransient<CSVImportService>();
builder.Services.AddTransient<JSONImportService>();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<IDatasetVersionRepository, DatasetVersionRepository>();
builder.Services.AddScoped<IDatasetColumnRepository, DatasetColumnRepository>();
builder.Services.AddScoped<IDataRecordRepository, DataRecordRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

var app = builder.Build();

app.MapControllers();

app.Run();