namespace GraderLogic.DemoData;

/// <summary>
/// Use cases for demonstrating and trying out the app.
/// </summary>
public interface IDemoDataService
{
    /// <summary>
    /// Deletes all data and fills the database with generated demo data (see <see cref="Data.TestDataGenerator"/>),
    /// all in one transaction.
    /// </summary>
    Task ReplaceWithDemoDataAsync(CancellationToken cancellationToken);
}
