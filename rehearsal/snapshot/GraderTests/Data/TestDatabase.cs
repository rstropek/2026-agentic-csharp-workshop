using GraderLogic.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Data;

/// <summary>
/// Private in-memory SQLite database with all migrations applied. It lives as long as this object
/// (the open connection keeps it alive).
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public TestDatabase()
    {
        connection.Open();

        // Migrate (not EnsureCreated) tests the migrations. It also fails if the model has changes
        // that no migration covers (PendingModelChangesWarning).
        using var dbContext = CreateContext();
        dbContext.Database.Migrate();
    }

    /// <summary>
    /// Creates a new context (with an empty change tracker) for the database.
    /// </summary>
    public GraderDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GraderDbContext>().UseSqlite(connection).Options);

    public void Dispose() => connection.Dispose();
}
