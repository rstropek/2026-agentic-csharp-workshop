using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GraderLogic.Data;

namespace GraderLogic.ClassManagement;

[Description("A school year to pick from.")]
public record SchoolYearSummary(
    [property: Description("Id of the school year.")] int Id,
    [property: Description("Name of the school year, e.g. 2025/26.")] string Name);

[Description("A class in the list of classes of a school year.")]
public record ClassSummary(
    [property: Description("Id of the class.")] int Id,
    [property: Description("Name of the class, e.g. 1A.")] string Name,
    [property: Description("Number of students in the class.")] int StudentCount);

[Description("A class incl. its students, sorted by last name and first name.")]
public record ClassDetails(
    [property: Description("Id of the class.")] int Id,
    [property: Description("Name of the class, e.g. 1A.")] string Name,
    [property: Description("Id of the school year the class belongs to.")] int SchoolYearId,
    [property: Description("Name of the school year the class belongs to.")] string SchoolYearName,
    [property: Description("Students of the class, sorted by last name and first name.")] IReadOnlyList<ClassStudent> Students);

[Description("A student of a class.")]
public record ClassStudent(
    [property: Description("Id of the student.")] int Id,
    [property: Description("First name of the student.")] string FirstName,
    [property: Description("Last name of the student.")] string LastName);

[Description("A class incl. the complete list of its students. Existing students that are missing in the list are removed.")]
public record SaveClassRequest(
    [property: Description("Name of the class, unique within the school year.")]
    [property: Required, MaxLength(SchoolClass.NameMaxLength)]
    string Name,
    [property: Description("All students of the class.")]
    [property: Required]
    IReadOnlyList<SaveClassStudent> Students);

[Description("A student of a class to save.")]
public record SaveClassStudent(
    [property: Description("Id of an existing student of the class; null for a new student.")] int? Id,
    [property: Description("First name of the student.")]
    [property: Required, MaxLength(Student.FirstNameMaxLength)]
    string FirstName,
    [property: Description("Last name of the student.")]
    [property: Required, MaxLength(Student.LastNameMaxLength)]
    string LastName);
