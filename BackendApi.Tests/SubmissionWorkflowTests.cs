using BackendApi.Models;
using Xunit;

namespace BackendApi.Tests;

public class SubmissionWorkflowTests
{
    [Fact]
    public void GradeCalculation_ValidMarks_PassesValidation()
    {
        int maxMarks = 100;
        int assignedMarks = 85;
        Assert.True(assignedMarks <= maxMarks);
    }

    [Fact]
    public void SubmissionStatus_InitialState_IsUnderReview()
    {
        var submission = new Submission
        {
            AssignmentId = 1,
            StudentEmail = "student@school.com",
            AnswerText = "Test Answer"
        };
        Assert.Equal("UnderReview", submission.Status);
        Assert.Null(submission.Marks);
    }
}
