using System.ComponentModel.DataAnnotations;

namespace BackendApi.Models;

public class Assignment
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int MaxMarks { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}