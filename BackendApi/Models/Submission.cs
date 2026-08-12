namespace BackendApi.Models;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}