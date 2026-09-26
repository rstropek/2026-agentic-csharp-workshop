using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// EF Core context of the grader database (SQLite). See tech-docs/database.md.
/// </summary>
/// <remarks>
/// All constraints of the data model are enforced by the database. Simple facets (max length, unique indexes)
/// are defined with attributes on the entity classes, everything else here.
/// </remarks>
public class GraderDbContext(DbContextOptions<GraderDbContext> options) : DbContext(options)
{
    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();

    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Exam> Exams => Set<Exam>();

    public DbSet<ExamResult> ExamResults => Set<ExamResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExamResult>().ToTable(table => table.HasCheckConstraint(
            "CK_ExamResults_Percentage",
            string.Create(
                CultureInfo.InvariantCulture,
                $"\"{nameof(ExamResult.Percentage)}\" BETWEEN {ExamResult.MinPercentage} AND {ExamResult.MaxPercentage}")));

        // No cascading deletes: a referenced record must not be deleted. Applies to all relationships,
        // so it has to stay at the end of this method.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
