using BackendApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<ClassEntity> Classes => Set<ClassEntity>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAllocation> TeacherAllocations => Set<TeacherAllocation>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
}