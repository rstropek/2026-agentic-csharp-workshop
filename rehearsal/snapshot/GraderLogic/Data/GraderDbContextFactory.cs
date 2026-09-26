using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GraderLogic.Data;

/// <summary>
/// Creates the context for the EF Core tools (<c>dotnet ef</c>), so migrations can be generated and applied
/// from the GraderLogic project without a startup project.
/// </summary>
public class GraderDbContextFactory : IDesignTimeDbContextFactory<GraderDbContext>
{
    public GraderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GraderDbContext>()
            .UseSqlite(GraderDatabase.GetDefaultConnectionString())
            .Options;
        return new GraderDbContext(options);
    }
}
