namespace BackendApi.DTOs.Admin;

public record CreateUserDto(string Email, string Password, string Role, int? ClassId);
public record UpdateUserDto(string Email, string? Password, string Role, int? ClassId);
public record UserResponseDto(int Id, string Email, string Role, int? ClassId, string? ClassName);

public record UpsertClassDto(string Name, string Section);
public record UpsertSubjectDto(string Name, string Code);

public record AssignTeacherDto(string TeacherEmail, int ClassId, int SubjectId);