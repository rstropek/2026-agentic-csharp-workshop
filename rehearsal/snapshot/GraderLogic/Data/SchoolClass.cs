using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// A class of students in a school year (e.g. <c>1A</c>).
/// </summary>
[Index(nameof(SchoolYearId), nameof(Name), IsUnique = true)]
public class SchoolClass
{
    public const int NameMaxLength = 50;

    public int Id { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }

    public int SchoolYearId { get; set; }

    public SchoolYear SchoolYear { get; set; } = null!;

    public List<Student> Students { get; set; } = [];

    public List<Exam> Exams { get; set; } = [];
}
