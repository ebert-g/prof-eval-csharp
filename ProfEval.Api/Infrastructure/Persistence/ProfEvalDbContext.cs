using Microsoft.EntityFrameworkCore;
using ProfEval.Api.Domain.Entities;

namespace ProfEval.Api.Infrastructure.Persistence;

public class ProfEvalDbContext : DbContext
{
    public ProfEvalDbContext(DbContextOptions<ProfEvalDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Professor> Professors { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Student configuration
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Registration).HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasMany(e => e.Evaluations)
                .WithOne(e => e.Student)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Professor configuration
        modelBuilder.Entity<Professor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.Specialization).HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasMany(e => e.Evaluations)
                .WithOne(e => e.Professor)
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Evaluation configuration
        modelBuilder.Entity<Evaluation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).HasPrecision(5, 2);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.AnonymousToken).HasMaxLength(100);
            entity.HasIndex(e => e.AnonymousToken).IsUnique();
            entity.HasIndex(e => new { e.StudentId, e.ProfessorId });
            entity.HasOne(e => e.Student)
                .WithMany(s => s.Evaluations)
                .HasForeignKey(e => e.StudentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Professor)
                .WithMany(p => p.Evaluations)
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VerificationCode configuration
        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
            entity.HasIndex(e => new { e.Email, e.Code });
        });
    }
}
