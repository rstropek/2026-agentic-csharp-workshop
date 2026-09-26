using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// A subject (e.g. <c>Mathematics</c>).
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class Course
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public List<Exam> Exams { get; set; } = [];
}
