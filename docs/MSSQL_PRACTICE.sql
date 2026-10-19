/*
  SQL Server practice for the real EF Core schema in this repository.
  PostgreSQL remains the application's runtime database. These queries are
  learning examples and are not executed by the application.
*/

-- 1. Published assignments whose deadline has not passed (SELECT, WHERE, ORDER BY).
SELECT a.Id, a.Title, a.DueDate, a.MaxMarks
FROM dbo.Assignments AS a
WHERE a.IsDraft = 0
  AND (a.DueDate IS NULL OR a.DueDate >= SYSUTCDATETIME())
ORDER BY a.DueDate, a.Title;

-- 2. Submission count for each assignment, including assignments with none (LEFT JOIN, GROUP BY, COUNT).
SELECT a.Id, a.Title, COUNT(s.Id) AS SubmissionCount
FROM dbo.Assignments AS a
LEFT JOIN dbo.Submissions AS s ON s.AssignmentId = a.Id
GROUP BY a.Id, a.Title
ORDER BY SubmissionCount DESC, a.Title;

-- 3. Only assignments that have at least five submissions (HAVING).
SELECT a.Id, a.Title, COUNT(s.Id) AS SubmissionCount
FROM dbo.Assignments AS a
INNER JOIN dbo.Submissions AS s ON s.AssignmentId = a.Id
GROUP BY a.Id, a.Title
HAVING COUNT(s.Id) >= 5
ORDER BY SubmissionCount DESC;

-- 4. Average awarded mark and percentage by subject (INNER JOIN, AVG).
SELECT sub.Id,
       sub.Name,
       AVG(CAST(s.Marks AS decimal(10, 2))) AS AverageMarks,
       AVG(100.0 * s.Marks / NULLIF(a.MaxMarks, 0)) AS AveragePercentage
FROM dbo.Subjects AS sub
INNER JOIN dbo.Assignments AS a ON a.SubjectId = sub.Id
INNER JOIN dbo.Submissions AS s ON s.AssignmentId = a.Id
WHERE s.Status = 'Graded' AND s.Marks IS NOT NULL
GROUP BY sub.Id, sub.Name
ORDER BY AveragePercentage DESC;

-- 5. Students in a class who have not submitted one selected assignment.
DECLARE @AssignmentId int = 1;
SELECT u.Id, u.Email
FROM dbo.Users AS u
INNER JOIN dbo.Assignments AS a ON a.Id = @AssignmentId AND a.ClassId = u.ClassId
LEFT JOIN dbo.Submissions AS s
  ON s.AssignmentId = a.Id AND s.StudentEmail = u.Email
WHERE u.Role = 'Student' AND s.Id IS NULL
ORDER BY u.Email;

-- 6. Teacher to class to subject allocations.
SELECT ta.TeacherEmail,
       c.Name AS ClassName,
       c.Section,
       sub.Name AS SubjectName,
       sub.Code AS SubjectCode
FROM dbo.TeacherAllocations AS ta
INNER JOIN dbo.Classes AS c ON c.Id = ta.ClassId
INNER JOIN dbo.Subjects AS sub ON sub.Id = ta.SubjectId
ORDER BY ta.TeacherEmail, c.Name, c.Section, sub.Code;

-- 7. Number of graded submissions by class.
SELECT c.Id, c.Name, c.Section, COUNT(s.Id) AS GradedSubmissionCount
FROM dbo.Classes AS c
LEFT JOIN dbo.Assignments AS a ON a.ClassId = c.Id
LEFT JOIN dbo.Submissions AS s ON s.AssignmentId = a.Id AND s.Status = 'Graded'
GROUP BY c.Id, c.Name, c.Section
ORDER BY GradedSubmissionCount DESC;

-- 8. Page two of audit events, newest first (SQL Server pagination).
SELECT ae.Id, ae.Action, ae.EntityType, ae.EntityId,
       ae.PerformedByUserId, ae.PerformedByRole, ae.CreatedAtUtc
FROM dbo.AuditEvents AS ae
ORDER BY ae.CreatedAtUtc DESC, ae.Id DESC
OFFSET 25 ROWS FETCH NEXT 25 ROWS ONLY;

-- 9. Top-level activity counts for the last 30 days.
SELECT ae.Action, COUNT(*) AS EventCount
FROM dbo.AuditEvents AS ae
WHERE ae.CreatedAtUtc >= DATEADD(day, -30, SYSUTCDATETIME())
GROUP BY ae.Action
ORDER BY EventCount DESC, ae.Action;

-- 10. One reporting stored procedure. Business writes remain in EF Core services/controllers.
CREATE OR ALTER PROCEDURE dbo.GetSubmissionSummaryForAssignment
    @AssignmentId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.Id AS AssignmentId,
           a.Title,
           a.MaxMarks,
           COUNT(s.Id) AS TotalSubmissions,
           SUM(CASE WHEN s.Status = 'UnderReview' THEN 1 ELSE 0 END) AS UnderReviewCount,
           SUM(CASE WHEN s.Status = 'Graded' THEN 1 ELSE 0 END) AS GradedCount,
           AVG(CASE WHEN s.Status = 'Graded'
                    THEN 100.0 * s.Marks / NULLIF(a.MaxMarks, 0)
               END) AS AverageGradedPercentage
    FROM dbo.Assignments AS a
    LEFT JOIN dbo.Submissions AS s ON s.AssignmentId = a.Id
    WHERE a.Id = @AssignmentId
    GROUP BY a.Id, a.Title, a.MaxMarks;
END;
GO

-- Example execution:
EXEC dbo.GetSubmissionSummaryForAssignment @AssignmentId = 1;
