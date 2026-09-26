using System.ComponentModel.DataAnnotations;

namespace GraderLogic.Data;

public class Student
{
    public const int FirstNameMaxLength = 50;

    public const int LastNameMaxLength = 75;

    public int Id { get; set; }

    [MaxLength(FirstNameMaxLength)]
    public required string FirstName { get; set; }

    [MaxLength(LastNameMaxLength)]
    public required string LastName { get; set; }

    public int SchoolClassId { get; set; }

    public SchoolClass SchoolClass { get; set; } = null!;

    public List<ExamResult> ExamResults { get; set; } = [];
}
