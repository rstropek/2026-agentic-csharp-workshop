using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// An exam a school class takes in a course on a given date.
/// </summary>
[Index(nameof(CourseId), nameof(SchoolClassId), nameof(ExamDate), IsUnique = true)]
public class Exam
{
    public int Id { get; set; }

    public DateOnly ExamDate { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public int SchoolClassId { get; set; }

    public SchoolClass SchoolClass { get; set; } = null!;

    public List<ExamResult> ExamResults { get; set; } = [];
}
