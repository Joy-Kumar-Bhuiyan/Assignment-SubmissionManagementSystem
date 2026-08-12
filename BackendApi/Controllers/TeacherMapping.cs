namespace BackendApi.Models;

public class TeacherMapping
{
    public int Id { get; set; }
    public string TeacherEmail { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public Class? Class { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
}