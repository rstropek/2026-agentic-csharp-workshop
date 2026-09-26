using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Data;

[Trait("Category", "Integration")]
public sealed class TestDataGeneratorTests : IDisposable
{
    private const int Seed = 42;

    private readonly TestDatabase database = new();

    private readonly FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task GenerateAsync_EmptyDatabase_CreatesExpectedRowCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var dbContext = database.CreateContext())
        {
            await new TestDataGenerator(dbContext, timeProvider, Seed).GenerateAsync(ct);
        }

        await using var verifyContext = database.CreateContext();
        // 3 school years, each with 3 + 3 + 2 + 2 classes of 20 students; 5 courses with 4 exams per class;
        // 20 - 2 results per exam.
        Assert.Equal(3, await verifyContext.SchoolYears.CountAsync(ct));
        Assert.Equal(30, await verifyContext.SchoolClasses.CountAsync(ct));
        Assert.Equal(600, await verifyContext.Students.CountAsync(ct));
        Assert.Equal(5, await verifyContext.Courses.CountAsync(ct));
        Assert.Equal(600, await verifyContext.Exams.CountAsync(ct));
        Assert.Equal(10_800, await verifyContext.ExamResults.CountAsync(ct));
    }

    [Fact]
    public async Task GenerateAsync_EmptyDatabase_CreatesSchoolYearsEndingWithCurrentYear()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = database.CreateContext();

        await new TestDataGenerator(dbContext, timeProvider, Seed).GenerateAsync(ct);

        var names = await dbContext.SchoolYears.OrderBy(schoolYear => schoolYear.Name).Select(schoolYear => schoolYear.Name).ToListAsync(ct);
        Assert.Equal(["2024/25", "2025/26", "2026/27"], names);
    }

    [Fact]
    public async Task GenerateAsync_EmptyDatabase_CreatesPercentagesWithExpectedDistribution()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = database.CreateContext();

        await new TestDataGenerator(dbContext, timeProvider, Seed).GenerateAsync(ct);

        var percentages = await dbContext.ExamResults.Select(result => result.Percentage).ToListAsync(ct);
        Assert.InRange(percentages.Average(), 74, 76);
        Assert.InRange(percentages.Count(percentage => percentage >= 90) / (double)percentages.Count, 0.08, 0.12);
    }

    [Fact]
    public async Task GenerateAsync_DatabaseNotEmpty_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = database.CreateContext();
        dbContext.Courses.Add(new Course { Name = "Existing Course" });
        await dbContext.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new TestDataGenerator(dbContext, timeProvider, Seed).GenerateAsync(ct));
        Assert.Equal(1, await dbContext.Courses.CountAsync(ct));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
