namespace BackendApi.Models;

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty; // e.g., "AcademicYear", "AllowLateSubmissions"
    public string Value { get; set; } = string.Empty; // e.g., "2026-2027", "true"
}