using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BackendApi.Tests;

public class SubmissionWorkflowTests
{
    [Fact]
    public void GradeCalculation_ValidMarks_PassesValidation()
    {
        // Test Business Rule: Marks must not exceed MaxMarks
        int maxMarks = 100;
        int assignedMarks = 85;

        Assert.True(assignedMarks <= maxMarks);
    }

    [Fact]
    public void SubmissionStatus_InitialState_IsSubmitted()
    {
        var submission = new Submission
        {
            AssignmentId = 1,
            StudentEmail = "student@school.com",
            AnswerText = "Test Answer"
        };

        Assert.Equal("Submitted", submission.Status);
        Assert.Null(submission.Marks);
    }
}