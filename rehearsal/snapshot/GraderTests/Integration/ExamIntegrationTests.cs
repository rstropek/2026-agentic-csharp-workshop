using System.Net;
using GraderLogic.Data;
using GraderLogic.Exams;
using Microsoft.AspNetCore.Mvc;

namespace GraderTests.Integration;

/// <summary>
/// Integration tests for the exam endpoints: real API, real logic, in-memory SQLite database.
/// </summary>
[Trait("Category", "Integration")]
public class ExamIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task SaveExam_ChangedResults_ReturnsExamWithResultsOfAllStudents()
    {
        var ct = TestContext.Current.CancellationToken;
        var (exam, ada, alan) = await ArrangeExamAsync("2025/26", ct);
        SaveExamRequest request = new(new DateOnly(2025, 11, 3), exam.CourseId, [new SaveExamResult(alan.Id, 55.5), new SaveExamResult(ada.Id, null)]);

        var response = await factory.CreateClient().PutAsJsonAsync($"/api/exams/{exam.Id}", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ExamDetails>(ct);
        Assert.Equal(new DateOnly(2025, 11, 3), result?.ExamDate);
        Assert.Equal("2025/26", result?.SchoolYearName);
        Assert.Equal(
            [new ExamStudentResult(ada.Id, "Ada", "Lovelace", null), new ExamStudentResult(alan.Id, "Alan", "Turing", 55.5)],
            result?.Results);
    }

    [Fact]
    public async Task SaveExam_ResultForStudentOfOtherClass_ReturnsBadRequestWithCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var (exam, _, _) = await ArrangeExamAsync("2024/25", ct);
        var (_, otherStudent, _) = await ArrangeExamAsync("2023/24", ct);
        SaveExamRequest request = new(exam.ExamDate, exam.CourseId, [new SaveExamResult(otherStudent.Id, 90)]);

        var response = await factory.CreateClient().PutAsJsonAsync($"/api/exams/{exam.Id}", request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("student-not-in-class", problem?.Extensions["code"]?.ToString());
    }

    /// <summary>
    /// Creates a class with two students and an exam in a new school year and course, in which Ada has a result
    /// (the database is shared by all tests of this class).
    /// </summary>
    private async Task<(Exam Exam, Student Ada, Student Alan)> ArrangeExamAsync(string schoolYearName, CancellationToken ct)
    {
        await using var dbContext = factory.CreateDbContext();
        var ada = new Student { FirstName = "Ada", LastName = "Lovelace" };
        var alan = new Student { FirstName = "Alan", LastName = "Turing" };
        var schoolClass = new SchoolClass { Name = "1A", SchoolYear = new SchoolYear { Name = schoolYearName }, Students = [alan, ada] };
        var exam = new Exam
        {
            ExamDate = new DateOnly(2025, 10, 15),
            Course = new Course { Name = $"Mathematics {schoolYearName}" },
            SchoolClass = schoolClass,
            ExamResults = [new ExamResult { Student = ada, Percentage = 80 }],
        };
        dbContext.Exams.Add(exam);
        await dbContext.SaveChangesAsync(ct);
        return (exam, ada, alan);
    }
}
