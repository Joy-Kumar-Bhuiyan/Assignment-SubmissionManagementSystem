using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup CORS
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// 2. Setup PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Setup JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecretKeyForJwtAuthentication12345!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// --- ENDPOINTS ---

// Auth Endpoint: Validates against database seed users
app.MapPost("/api/login", async (LoginDto login, AppDbContext db) => {
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == login.Email && u.Password == login.Password);
    if (user == null) return Results.Unauthorized();

    var claims = new[] {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddDays(1),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), 
            SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token), Role = user.Role, Email = user.Email });
});

// Admin Endpoint: Get all system users
app.MapGet("/api/admin/users", async (AppDbContext db) => {
    return await db.Users.Select(u => new { u.Id, u.Email, u.Role }).ToListAsync();
}).RequireAuthorization(p => p.RequireRole("Admin"));

// Teacher Endpoint: Create Assignment
app.MapPost("/api/assignments", async (Assignment task, AppDbContext db) => {
    db.Assignments.Add(task);
    await db.SaveChangesAsync();
    return Results.Ok(task);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

// Shared Endpoint: Get Assignments
app.MapGet("/api/assignments", async (AppDbContext db) => {
    return await db.Assignments.ToListAsync();
}).RequireAuthorization();

// Student Endpoint: Submit Assignment
app.MapPost("/api/submissions", async (Submission sub, ClaimsPrincipal user, AppDbContext db) => {
    var studentEmail = user.Identity?.Name ?? sub.StudentEmail;
    sub.StudentEmail = studentEmail;
    sub.Status = "Submitted";
    db.Submissions.Add(sub);
    await db.SaveChangesAsync();
    return Results.Ok(sub);
}).RequireAuthorization(p => p.RequireRole("Student"));

// Student Endpoint: Get Own Submissions
app.MapGet("/api/submissions/my", async (ClaimsPrincipal user, AppDbContext db) => {
    var studentEmail = user.Identity?.Name;
    var list = await db.Submissions.Where(s => s.StudentEmail == studentEmail).ToListAsync();
    return Results.Ok(list);
}).RequireAuthorization(p => p.RequireRole("Student"));

// Teacher Endpoint: Get All Submissions for Grading
app.MapGet("/api/submissions", async (AppDbContext db) => {
    return await db.Submissions.ToListAsync();
}).RequireAuthorization(p => p.RequireRole("Teacher", "Admin"));

// Teacher Endpoint: Grade Submission with Marks and Feedback
app.MapPut("/api/submissions/{id}/grade", async (int id, GradeDto grade, AppDbContext db) => {
    var sub = await db.Submissions.FindAsync(id);
    if (sub == null) return Results.NotFound();
    
    sub.Marks = grade.Marks;
    sub.Feedback = grade.Feedback;
    sub.Status = "Graded";
    await db.SaveChangesAsync();
    return Results.Ok(sub);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.Run("http://localhost:5000");

// --- ENTITIES & DB CONTEXT ---
public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        // Seed Demo Accounts (Admin, Teacher, Student)
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Email = "admin@school.com", Password = "AdminPassword123!", Role = "Admin" },
            new User { Id = 2, Email = "teacher@school.com", Password = "TeacherPassword123!", Role = "Teacher" },
            new User { Id = 3, Email = "student@school.com", Password = "StudentPassword123!", Role = "Student" }
        );
    }
}

public class User {
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class Assignment {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMarks { get; set; } = 100;
}

public class Submission {
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public string Status { get; set; } = "Submitted";
}

public record LoginDto(string Email, string Password);
public record GradeDto(int Marks, string Feedback);