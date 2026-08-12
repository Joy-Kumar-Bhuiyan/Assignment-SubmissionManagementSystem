namespace BackendApi.Models;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Academic Structure Links (Nullable for backward compatibility)
    public int? ClassId { get; set; }
    public ClassEntity? Class { get; set; }

    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
}