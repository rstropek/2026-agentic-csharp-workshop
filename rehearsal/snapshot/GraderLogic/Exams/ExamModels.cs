using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GraderLogic.Data;

namespace GraderLogic.Exams;

[Description("A course to pick from.")]
public record CourseSummary(
    [property: Description("Id of the course.")] int Id,
    [property: Description("Name of the course, e.g. Mathematics.")] string Name);

[Description("An exam in the list of exams of a class.")]
public record ExamSummary(
    [property: Description("Id of the exam.")] int Id,
    [property: Description("Date of the exam.")] DateOnly ExamDate,
    [property: Description("Id of the course of the exam.")] int CourseId,
    [property: Description("Name of the course of the exam.")] string CourseName);

[Description("An exam incl. the results of all students of the class.")]
public record ExamDetails(
    [property: Description("Id of the exam.")] int Id,
    [property: Description("Date of the exam.")] DateOnly ExamDate,
    [property: Description("Id of the course of the exam.")] int CourseId,
    [property: Description("Name of the course of the exam.")] string CourseName,
    [property: Description("Id of the class that took the exam.")] int ClassId,
    [property: Description("Name of the class that took the exam.")] string ClassName,
    [property: Description("Id of the school year of the class.")] int SchoolYearId,
    [property: Description("Name of the school year of the class.")] string SchoolYearName,
    [property: Description("One entry per student of the class, sorted by last name and first name.")]
    IReadOnlyList<ExamStudentResult> Results);

[Description("The result of a student in an exam.")]
public record ExamStudentResult(
    [property: Description("Id of the student.")] int StudentId,
    [property: Description("First name of the student.")] string FirstName,
    [property: Description("Last name of the student.")] string LastName,
    [property: Description("Percentage (0 to 100); null if the student has no result.")] double? Percentage);

[Description("An exam incl. the complete list of its results. Existing results that are missing in the list (or null) are removed.")]
public record SaveExamRequest(
    [property: Description("Date of the exam.")] DateOnly ExamDate,
    [property: Description("Id of the course of the exam.")] int CourseId,
    [property: Description("Results of the students of the class; at most one per student.")]
    [property: Required]
    IReadOnlyList<SaveExamResult> Results);

[Description("The result of a student to save.")]
public record SaveExamResult(
    [property: Description("Id of a student of the exam's class.")] int StudentId,
    [property: Description("Percentage (0 to 100); null if the student has no result.")]
    [property: Range(ExamResult.MinPercentage, ExamResult.MaxPercentage)]
    double? Percentage);
