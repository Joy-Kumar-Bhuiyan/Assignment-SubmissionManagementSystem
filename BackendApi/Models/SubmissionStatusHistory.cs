namespace BackendApi.Models;

public sealed class SubmissionStatusHistory
{
    public long Id { get; set; }
    public int SubmissionId { get; set; }
    public Submission? Submission { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public string ChangedByRole { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
