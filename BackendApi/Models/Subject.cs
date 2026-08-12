namespace BackendApi.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Mathematics"
    public string Code { get; set; } = string.Empty; // e.g., "MATH101"
}