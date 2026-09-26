using GraderLogic;
using GraderLogic.ClassManagement;
using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Data;

/// <summary>
/// Integration tests for <see cref="ClassManagementService"/> against SQLite.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClassManagementServiceTests : IDisposable
{
    private readonly TestDatabase database = new();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task SaveClassAsync_AddedChangedAndRemovedStudents_SavesCompleteList()
    {
        var ct = TestContext.Current.CancellationToken;
        var schoolClass = await ArrangeClassAsync(ct);
        var (ada, alan, grace) = (schoolClass.Students[0], schoolClass.Students[1], schoolClass.Students[2]);
        SaveClassRequest request = new("1B",
        [
            new SaveClassStudent(ada.Id, "Ada", "King"),        // changed
            new SaveClassStudent(grace.Id, "Grace", "Hopper"),  // unchanged
            new SaveClassStudent(null, "Barbara", "Liskov"),    // added; Alan is removed
        ]);

        ClassDetails result;
        await using (var dbContext = database.CreateContext())
        {
            result = await new ClassManagementService(dbContext).SaveClassAsync(schoolClass.Id, request, ct);
        }

        Assert.Equal("1B", result.Name);
        Assert.Equal(["Hopper", "King", "Liskov"], result.Students.Select(student => student.LastName));
        await using var verifyContext = database.CreateContext();
        var students = await verifyContext.Students
            .Where(student => student.SchoolClassId == schoolClass.Id)
            .OrderBy(student => student.LastName)
            .ToListAsync(ct);
        Assert.Equal(["Hopper", "King", "Liskov"], students.Select(student => student.LastName));
        Assert.Equal(ada.Id, students.Single(student => student.LastName == "King").Id);
        Assert.Equal(result.Students.Select(student => student.Id), students.Select(student => student.Id));
        Assert.False(await verifyContext.Students.AnyAsync(student => student.Id == alan.Id, ct));
    }

    [Fact]
    public async Task SaveClassAsync_RemovedStudentHasExamResults_ThrowsConflictAndSavesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var schoolClass = await ArrangeClassAsync(ct);
        var (ada, alan) = (schoolClass.Students[0], schoolClass.Students[1]);
        await using (var arrangeContext = database.CreateContext())
        {
            arrangeContext.Exams.Add(new Exam
            {
                ExamDate = new DateOnly(2025, 10, 15),
                Course = new Course { Name = "Mathematics" },
                SchoolClassId = schoolClass.Id,
                ExamResults = [new ExamResult { StudentId = alan.Id, Percentage = 80 }],
            });
            await arrangeContext.SaveChangesAsync(ct);
        }

        // Alan (with a result) is missing, so he would be removed; Ada's change must not be saved either.
        SaveClassRequest request = new("1A", [new SaveClassStudent(ada.Id, "Ada", "King")]);

        await using (var dbContext = database.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => new ClassManagementService(dbContext).SaveClassAsync(schoolClass.Id, request, ct));
            Assert.Equal("student-has-results", ex.Code);
        }

        await using var verifyContext = database.CreateContext();
        Assert.Equal(3, await verifyContext.Students.CountAsync(student => student.SchoolClassId == schoolClass.Id, ct));
        Assert.Equal("Lovelace", (await verifyContext.Students.SingleAsync(student => student.Id == ada.Id, ct)).LastName);
    }

    private async Task<SchoolClass> ArrangeClassAsync(CancellationToken ct)
    {
        await using var dbContext = database.CreateContext();
        var schoolClass = new SchoolClass
        {
            Name = "1A",
            SchoolYear = new SchoolYear { Name = "2025/26" },
            Students =
            [
                new Student { FirstName = "Ada", LastName = "Lovelace" },
                new Student { FirstName = "Alan", LastName = "Turing" },
                new Student { FirstName = "Grace", LastName = "Hopper" },
            ],
        };
        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(ct);
        return schoolClass;
    }
}
