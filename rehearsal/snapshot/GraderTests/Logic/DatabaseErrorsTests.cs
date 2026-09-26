using GraderLogic;
using GraderLogic.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Logic;

public class DatabaseErrorsTests
{
    [Fact]
    public void TryTranslate_ConstraintViolation_ReturnsConflictWithCode()
    {
        var exception = new DbUpdateException("Save failed.", new SqliteException("UNIQUE constraint failed: SchoolClasses.Name", 19));

        var translated = DatabaseErrors.TryTranslate(exception, out var result);

        Assert.True(translated);
        var conflict = Assert.IsType<ConflictException>(result);
        Assert.Equal(DatabaseErrors.ConstraintViolationCode, conflict.Code);
        Assert.Same(exception, conflict.InnerException);
    }

    [Fact]
    public void TryTranslate_OtherDatabaseError_IsNotTranslated()
    {
        // SQLITE_BUSY: unexpected, must surface as a 500 instead of being disguised as a conflict.
        var exception = new DbUpdateException("Save failed.", new SqliteException("database is locked", 5));

        var translated = DatabaseErrors.TryTranslate(exception, out var result);

        Assert.False(translated);
        Assert.Null(result);
    }
}
