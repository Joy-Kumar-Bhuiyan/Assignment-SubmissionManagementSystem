using BackendApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ClassEntity> Classes => Set<ClassEntity>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAllocation> TeacherAllocations => Set<TeacherAllocation>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClassEntity>().ToTable("Classes");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(254);
            entity.Property(u => u.Role).HasMaxLength(16);
        });

        modelBuilder.Entity<ClassEntity>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.Property(c => c.Section).HasMaxLength(50);
            entity.HasIndex(c => new { c.Name, c.Section }).IsUnique();
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(150);
            entity.Property(s => s.Code).HasMaxLength(30);
            entity.HasIndex(s => s.Code).IsUnique();
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(a => a.Title).HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(10_000);
            entity.ToTable(t => t.HasCheckConstraint("CK_Assignments_MaxMarks", "\"MaxMarks\" > 0 AND \"MaxMarks\" <= 10000"));
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.Property(s => s.StudentEmail).HasMaxLength(254);
            entity.Property(s => s.AnswerText).HasMaxLength(50_000);
            entity.Property(s => s.Status).HasMaxLength(30);
            entity.Property(s => s.Feedback).HasMaxLength(5_000);
            entity.HasIndex(s => new { s.AssignmentId, s.StudentEmail }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Submissions_Marks", "\"Marks\" IS NULL OR \"Marks\" >= 0"));
        });

        modelBuilder.Entity<TeacherAllocation>(entity =>
        {
            entity.Property(a => a.TeacherEmail).HasMaxLength(254);
            entity.HasIndex(a => new { a.TeacherEmail, a.ClassId, a.SubjectId }).IsUnique();
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.Property(s => s.Key).HasMaxLength(100);
            entity.Property(s => s.Value).HasMaxLength(2_000);
            entity.HasIndex(s => s.Key).IsUnique();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(a => a.Action).HasMaxLength(80);
            entity.Property(a => a.EntityType).HasMaxLength(80);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.PerformedByRole).HasMaxLength(16);
            entity.Property(a => a.Description).HasMaxLength(500);
            entity.HasIndex(a => a.CreatedAtUtc);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.PerformedByUserId);
        });
    }
}
