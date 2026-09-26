using Microsoft.Data.Sqlite;

namespace GraderLogic.Data;

public static class GraderDatabase
{
    /// <summary>
    /// Name of the file that marks the repository root.
    /// </summary>
    private const string RepositoryRootMarker = "Grader.slnx";

    /// <summary>
    /// Returns the connection string of the development database <c>data/grader.db</c> in the repository root
    /// and creates the <c>data</c> folder if necessary.
    /// </summary>
    /// <remarks>
    /// The repository root is searched upwards from the current directory, which is the project folder
    /// when the EF Core tools run.
    /// </remarks>
    public static string GetDefaultConnectionString()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, RepositoryRootMarker)))
        {
            directory = directory.Parent
                ?? throw new InvalidOperationException(
                    $"Repository root ({RepositoryRootMarker}) not found above {Directory.GetCurrentDirectory()}.");
        }

        var dataDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "data"));
        return new SqliteConnectionStringBuilder { DataSource = Path.Combine(dataDirectory.FullName, "grader.db") }.ToString();
    }
}
