using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetVersion> DatasetVersions => Set<DatasetVersion>();
    public DbSet<DatasetColumn> DatasetColumns => Set<DatasetColumn>();
    public DbSet<DataRecord> DataRecords => Set<DataRecord>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Dataset>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Datasets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Versions)
                .WithOne(v => v.Dataset)
                .HasForeignKey(v => v.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique();
        });

        modelBuilder.Entity<DatasetVersion>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Format)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.FileHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(e => new { e.DatasetId, e.Version })
                .IsUnique();

            entity.HasMany(e => e.Columns)
                .WithOne(c => c.DatasetVersion)
                .HasForeignKey(c => c.DatasetVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Records)
                .WithOne(r => r.DatasetVersion)
                .HasForeignKey(r => r.DatasetVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Reports)
                .WithOne(rep => rep.DatasetVersion)
                .HasForeignKey(rep => rep.DatasetVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DatasetColumn>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.DataType)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.DatasetVersionId, e.Name })
                .IsUnique();

            entity.HasIndex(e => new { e.DatasetVersionId, e.Position })
                .IsUnique();
        });

        modelBuilder.Entity<DataRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Data)
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.HasIndex(e => new { e.DatasetVersionId, e.RowNumber });
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ReportType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ResultData)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Operation)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ErrorType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.StackTrace)
                .HasMaxLength(8000);

            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.DatasetId);
        });
    }
}