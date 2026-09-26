using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.DemoData;

public class DemoDataService(GraderDbContext dbContext, TimeProvider timeProvider) : IDemoDataService
{
    public async Task ReplaceWithDemoDataAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Bulk deletes instead of loading all rows; dependents first because there are no cascading deletes.
        await dbContext.ExamResults.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Exams.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Students.ExecuteDeleteAsync(cancellationToken);
        await dbContext.SchoolClasses.ExecuteDeleteAsync(cancellationToken);
        await dbContext.SchoolYears.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Courses.ExecuteDeleteAsync(cancellationToken);

        await new TestDataGenerator(dbContext, timeProvider).GenerateAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
