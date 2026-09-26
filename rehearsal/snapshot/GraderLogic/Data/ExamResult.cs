using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// The result of a student in an exam.
/// </summary>
[Index(nameof(ExamId), nameof(StudentId), IsUnique = true)]
public class ExamResult
{
    public const double MinPercentage = 0;

    public const double MaxPercentage = 100;

    public int Id { get; set; }

    /// <summary>
    /// Percentage between <see cref="MinPercentage"/> and <see cref="MaxPercentage"/> (inclusive).
    /// </summary>
    public double Percentage { get; set; }

    public int ExamId { get; set; }

    public Exam Exam { get; set; } = null!;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;
}
