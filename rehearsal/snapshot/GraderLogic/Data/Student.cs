using System.ComponentModel.DataAnnotations;

namespace GraderLogic.Data;

public class Student
{
    public int Id { get; set; }

    [MaxLength(50)]
    public required string FirstName { get; set; }

    [MaxLength(75)]
    public required string LastName { get; set; }

    public int SchoolClassId { get; set; }

    public SchoolClass SchoolClass { get; set; } = null!;

    public List<ExamResult> ExamResults { get; set; } = [];
}
