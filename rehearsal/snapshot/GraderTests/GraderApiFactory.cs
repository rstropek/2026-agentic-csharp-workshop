using GraderLogic.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraderTests;

/// <summary>
/// Runs the API in memory against a private in-memory SQLite database, so tests never touch the
/// development database. The database lives as long as the factory (the open connection keeps it alive);
/// the API applies the migrations on startup.
/// </summary>
public sealed class GraderApiFactory : WebApplicationFactory<Program>
{
    // Named in-memory database with shared cache: all connections with this connection string see the same database.
    private readonly SqliteConnection keepAliveConnection =
        new($"Data Source=grader-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    public GraderApiFactory()
    {
        keepAliveConnection.Open();
    }

    /// <summary>
    /// Creates a new context for the API's database, e.g. to arrange or verify data.
    /// </summary>
    public GraderDbContext CreateDbContext()
    {
        // Accessing the services starts the API (if not started yet), which creates the schema.
        _ = Services;
        return new(new DbContextOptionsBuilder<GraderDbContext>().UseSqlite(keepAliveConnection.ConnectionString).Options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Grader", keepAliveConnection.ConnectionString);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            keepAliveConnection.Dispose();
        }
    }
}
