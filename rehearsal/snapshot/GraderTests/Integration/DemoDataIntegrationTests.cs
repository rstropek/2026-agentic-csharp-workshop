using System.Net;
using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Integration;

[Trait("Category", "Integration")]
public class DemoDataIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task ReplaceWithDemoData_DatabaseWithData_ReplacesAllData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var arrangeContext = factory.CreateDbContext())
        {
            arrangeContext.Courses.Add(new Course { Name = "Existing Course" });
            await arrangeContext.SaveChangesAsync(ct);
        }

        // Twice: the second call has to delete the demo data of the first one (all tables, incl. exam results).
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/demo-data", null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/demo-data", null, ct)).StatusCode);

        await using var verifyContext = factory.CreateDbContext();
        Assert.False(await verifyContext.Courses.AnyAsync(course => course.Name == "Existing Course", ct));
        Assert.Equal(TestDataGenerator.CourseNames.Count, await verifyContext.Courses.CountAsync(ct));
        Assert.Equal(TestDataGenerator.SchoolYearCount, await verifyContext.SchoolYears.CountAsync(ct));
    }
}
