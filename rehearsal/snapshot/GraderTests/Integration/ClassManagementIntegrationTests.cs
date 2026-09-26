using System.Net;
using GraderLogic.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Integration;

/// <summary>
/// Integration tests for the class management endpoints: real API, real logic, in-memory SQLite database.
/// </summary>
[Trait("Category", "Integration")]
public class ClassManagementIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task DeleteClass_WithoutExams_DeletesClassAndStudents()
    {
        var ct = TestContext.Current.CancellationToken;
        var classId = await ArrangeClassAsync("2025/26", withExam: false, ct);

        var response = await factory.CreateClient().DeleteAsync($"/api/classes/{classId}", ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var verifyContext = factory.CreateDbContext();
        Assert.False(await verifyContext.SchoolClasses.AnyAsync(schoolClass => schoolClass.Id == classId, ct));
        Assert.False(await verifyContext.Students.AnyAsync(student => student.SchoolClassId == classId, ct));
    }

    [Fact]
    public async Task DeleteClass_WithExams_ReturnsConflictWithCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var classId = await ArrangeClassAsync("2024/25", withExam: true, ct);

        var response = await factory.CreateClient().DeleteAsync($"/api/classes/{classId}", ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("class-has-exams", problem?.Extensions["code"]?.ToString());
        await using var verifyContext = factory.CreateDbContext();
        Assert.True(await verifyContext.SchoolClasses.AnyAsync(schoolClass => schoolClass.Id == classId, ct));
    }

    /// <summary>
    /// Creates a class with a student in a new school year (the database is shared by all tests of this class).
    /// </summary>
    private async Task<int> ArrangeClassAsync(string schoolYearName, bool withExam, CancellationToken ct)
    {
        await using var dbContext = factory.CreateDbContext();
        var schoolClass = new SchoolClass
        {
            Name = "1A",
            SchoolYear = new SchoolYear { Name = schoolYearName },
            Students = [new Student { FirstName = "Ada", LastName = "Lovelace" }],
        };
        if (withExam)
        {
            schoolClass.Exams.Add(new Exam { ExamDate = new DateOnly(2024, 10, 15), Course = new Course { Name = "Mathematics" } });
        }

        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(ct);
        return schoolClass.Id;
    }
}
