using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// Translates database errors into <see cref="GraderLogicException"/>s, so that <see cref="DbUpdateException"/>
/// and SQLite error codes never leave the data access code (see tech-docs/web-api.md).
/// </summary>
public static class DatabaseErrors
{
    /// <summary>
    /// Error code of constraint violations that were not caught by a business rule check (e.g. concurrent requests).
    /// </summary>
    public const string ConstraintViolationCode = "constraint-violation";

    // SQLITE_CONSTRAINT, see https://www.sqlite.org/rescode.html. Extended result codes are not distinguished:
    // ON DELETE RESTRICT reports SQLITE_CONSTRAINT_TRIGGER instead of SQLITE_CONSTRAINT_FOREIGNKEY.
    private const int SqliteConstraint = 19;

    /// <summary>
    /// Translates a known database error into a <see cref="GraderLogicException"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the error was translated; otherwise the error is unexpected.</returns>
    public static bool TryTranslate(DbUpdateException exception, [NotNullWhen(true)] out GraderLogicException? translated)
    {
        translated = exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint }
            ? new ConflictException(
                ConstraintViolationCode,
                "The change conflicts with existing data. Reload the data and try again.",
                exception)
            : null;
        return translated is not null;
    }

    /// <summary>
    /// Saves all changes of <paramref name="dbContext"/> and translates known database errors
    /// (see <see cref="TryTranslate"/>).
    /// </summary>
    public static async Task SaveChangesAndTranslateErrorsAsync(this DbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (TryTranslate(ex, out var translated))
        {
            throw translated;
        }
    }
}
