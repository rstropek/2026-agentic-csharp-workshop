using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// A school year spanning two calendar years (e.g. <c>2025/26</c>).
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class SchoolYear
{
    public int Id { get; set; }

    [MaxLength(30)]
    public required string Name { get; set; }

    public List<SchoolClass> SchoolClasses { get; set; } = [];
}
