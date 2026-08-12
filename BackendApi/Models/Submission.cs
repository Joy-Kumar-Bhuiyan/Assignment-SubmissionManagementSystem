using System.ComponentModel.DataAnnotations;

namespace BackendApi.Models;

public class Submission
{
    public int Id { get; set; }

    [Required]
    public int AssignmentId { get; set; }

    [Required, EmailAddress]
    public string StudentEmail { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string AnswerText { get; set; } = string.Empty;

    public int? Marks { get; set; }

    [MaxLength(1000)]
    public string? Feedback { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Submitted";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Assignment? Assignment { get; set; }
}