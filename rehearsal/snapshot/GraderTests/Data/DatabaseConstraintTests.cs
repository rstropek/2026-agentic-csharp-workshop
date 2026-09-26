using GraderLogic.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Data;

/// <summary>
/// Exemplary tests that constraints of the data model are enforced by the database (not only by EF Core).
/// </summary>
[Trait("Category", "Integration")]
public sealed class DatabaseConstraintTests : IDisposable
{
    // SQLITE_CONSTRAINT, see https://www.sqlite.org/rescode.html. The extended result code is not asserted:
    // ON DELETE RESTRICT reports SQLITE_CONSTRAINT_TRIGGER instead of SQLITE_CONSTRAINT_FOREIGNKEY.
    private const int SqliteConstraint = 19;

    private readonly TestDatabase database = new();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task AddSchoolYear_DuplicateName_Fails()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = database.CreateContext();
        dbContext.SchoolYears.Add(new SchoolYear { Name = "2025/26" });
        await dbContext.SaveChangesAsync(ct);

        dbContext.SchoolYears.Add(new SchoolYear { Name = "2025/26" });

        await AssertSqliteConstraintViolationAsync("UNIQUE constraint failed", () => dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task DeleteSchoolYear_WithSchoolClasses_Fails()
    {
        var ct = TestContext.Current.CancellationToken;
        int schoolYearId;
        await using (var arrangeContext = database.CreateContext())
        {
            var schoolYear = new SchoolYear { Name = "2025/26", SchoolClasses = [new SchoolClass { Name = "1A" }] };
            arrangeContext.SchoolYears.Add(schoolYear);
            await arrangeContext.SaveChangesAsync(ct);
            schoolYearId = schoolYear.Id;
        }

        // New context without tracked classes: EF Core would otherwise refuse the delete before reaching the database.
        await using var dbContext = database.CreateContext();
        dbContext.SchoolYears.Remove(await dbContext.SchoolYears.SingleAsync(schoolYear => schoolYear.Id == schoolYearId, ct));

        await AssertSqliteConstraintViolationAsync("FOREIGN KEY constraint failed", () => dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task AddExamResult_PercentageAbove100_Fails()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = database.CreateContext();
        var schoolClass = new SchoolClass { Name = "1A", SchoolYear = new SchoolYear { Name = "2025/26" } };
        var exam = new Exam { ExamDate = new DateOnly(2025, 10, 15), Course = new Course { Name = "Mathematics" }, SchoolClass = schoolClass };
        var student = new Student { FirstName = "Ada", LastName = "Lovelace", SchoolClass = schoolClass };
        dbContext.ExamResults.Add(new ExamResult { Percentage = 101, Exam = exam, Student = student });

        await AssertSqliteConstraintViolationAsync("CHECK constraint failed", () => dbContext.SaveChangesAsync(ct));
    }

    private static async Task AssertSqliteConstraintViolationAsync(string expectedMessage, Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(action);
        var sqliteException = Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Equal(SqliteConstraint, sqliteException.SqliteErrorCode);
        Assert.Contains(expectedMessage, sqliteException.Message, StringComparison.Ordinal);
    }
}
