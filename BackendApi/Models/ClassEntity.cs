namespace BackendApi.Models;

public class ClassEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Class 10"
    public string Section { get; set; } = string.Empty; // e.g., "A"
}