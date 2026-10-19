using System.Security.Claims;

namespace BackendApi.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Admin,
        Teacher,
        Student
    };
}

public static class SubmissionStatuses
{
    public const string UnderReview = "UnderReview";
    public const string Returned = "Returned";
    public const string Graded = "Graded";
    public const string Rejected = "Rejected";

    public static readonly HashSet<string> TeacherReviewOutcomes = new(StringComparer.Ordinal)
    {
        Returned,
        Rejected
    };
}

public static class ClaimsPrincipalExtensions
{
    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email) ??
        principal.FindFirstValue(ClaimTypes.Name) ??
        principal.Identity?.Name;
}

public static class InputNormalization
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
