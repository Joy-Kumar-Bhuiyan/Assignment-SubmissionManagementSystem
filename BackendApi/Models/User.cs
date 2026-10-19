namespace BackendApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TokenVersion { get; set; }

    // Class Enrollment for Students
    public int? ClassId { get; set; }
    public ClassEntity? Class { get; set; }
}
