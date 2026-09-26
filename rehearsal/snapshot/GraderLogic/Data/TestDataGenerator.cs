using Bogus;
using Microsoft.EntityFrameworkCore;

namespace GraderLogic.Data;

/// <summary>
/// Populates an empty grader database with sample data (see tech-docs/database.md).
/// </summary>
/// <param name="dbContext">Context of the database to populate.</param>
/// <param name="timeProvider">Provides the current year, which determines the generated school years.</param>
/// <param name="seed">Seed for reproducible data; random data if <see langword="null"/>.</param>
public class TestDataGenerator(GraderDbContext dbContext, TimeProvider timeProvider, int? seed = null)
{
    public const int SchoolYearCount = 3;

    public const int StudentsPerClass = 20;

    public const int ExamsPerCourseAndClass = 4;

    public const int StudentsWithoutResultPerExam = 2;

    /// <summary>
    /// Class letters per grade level (grade level = index + 1).
    /// </summary>
    public static readonly IReadOnlyList<string> ClassLettersPerGradeLevel = ["ABC", "ABC", "AB", "AB"];

    public static readonly IReadOnlyList<string> CourseNames =
        ["Mathematics", "English", "History", "Computer Science", "Physical Education"];

    /// <summary>
    /// Percentages follow a normal distribution with this mean, clamped to the valid range.
    /// </summary>
    public const double PercentageMean = 75;

    /// <summary>
    /// Standard deviation for which 10 % of the percentages are at least 90 (z-score of the 90th percentile is 1.2816).
    /// Clamping to 100 does not change that share.
    /// </summary>
    public const double PercentageStandardDeviation = (90 - PercentageMean) / 1.2816;

    private readonly Faker faker = new() { Random = seed is null ? new Randomizer() : new Randomizer(seed.Value) };

    /// <summary>
    /// Generates the sample data and saves it in one transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if any table of the database contains rows.</exception>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseIsEmptyAsync(cancellationToken);

        var courses = CourseNames.Select(name => new Course { Name = name }).ToList();
        dbContext.Courses.AddRange(courses);

        var currentYear = timeProvider.GetLocalNow().Year;
        for (var startYear = currentYear - SchoolYearCount + 1; startYear <= currentYear; startYear++)
        {
            var schoolYear = new SchoolYear { Name = $"{startYear}/{(startYear + 1) % 100:00}" };
            dbContext.SchoolYears.Add(schoolYear);

            for (var gradeLevel = 1; gradeLevel <= ClassLettersPerGradeLevel.Count; gradeLevel++)
            {
                foreach (var letter in ClassLettersPerGradeLevel[gradeLevel - 1])
                {
                    var schoolClass = new SchoolClass { Name = $"{gradeLevel}{letter}", SchoolYear = schoolYear };
                    schoolYear.SchoolClasses.Add(schoolClass);
                    AddStudents(schoolClass);
                    AddExams(schoolClass, courses, startYear);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDatabaseIsEmptyAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.SchoolYears.AnyAsync(cancellationToken)
            || await dbContext.SchoolClasses.AnyAsync(cancellationToken)
            || await dbContext.Students.AnyAsync(cancellationToken)
            || await dbContext.Courses.AnyAsync(cancellationToken)
            || await dbContext.Exams.AnyAsync(cancellationToken)
            || await dbContext.ExamResults.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Test data can only be generated into an empty database.");
        }
    }

    private void AddStudents(SchoolClass schoolClass)
    {
        for (var i = 0; i < StudentsPerClass; i++)
        {
            schoolClass.Students.Add(new Student
            {
                FirstName = faker.Name.FirstName(),
                LastName = faker.Name.LastName(),
                SchoolClass = schoolClass,
            });
        }
    }

    private void AddExams(SchoolClass schoolClass, List<Course> courses, int startYear)
    {
        // Exams are spread evenly between September and June: each exam lies in the middle of its share of the period.
        var firstDay = new DateOnly(startYear, 9, 1);
        var lastDay = new DateOnly(startYear + 1, 6, 30);
        var periodDays = lastDay.DayNumber - firstDay.DayNumber + 1;

        for (var courseIndex = 0; courseIndex < courses.Count; courseIndex++)
        {
            for (var examIndex = 0; examIndex < ExamsPerCourseAndClass; examIndex++)
            {
                // Courses are offset by a day each, so a class does not write all exams on one day; no exams on weekends.
                var examDate = firstDay.AddDays((int)((examIndex + 0.5) * periodDays / ExamsPerCourseAndClass) + courseIndex);
                examDate = examDate.DayOfWeek switch
                {
                    DayOfWeek.Saturday => examDate.AddDays(2),
                    DayOfWeek.Sunday => examDate.AddDays(1),
                    _ => examDate,
                };

                var exam = new Exam { ExamDate = examDate, Course = courses[courseIndex], SchoolClass = schoolClass };
                schoolClass.Exams.Add(exam);
                AddExamResults(exam, schoolClass.Students);
            }
        }
    }

    private void AddExamResults(Exam exam, List<Student> students)
    {
        var studentsWithResult = faker.PickRandom(students, students.Count - StudentsWithoutResultPerExam);
        foreach (var student in studentsWithResult)
        {
            exam.ExamResults.Add(new ExamResult { Percentage = NextPercentage(), Exam = exam, Student = student });
        }
    }

    private double NextPercentage()
    {
        // Box-Muller transform: standard normal value from two uniform values in (0, 1].
        var standardNormal = Math.Sqrt(-2 * Math.Log(1 - faker.Random.Double())) * Math.Cos(2 * Math.PI * faker.Random.Double());
        var percentage = PercentageMean + (PercentageStandardDeviation * standardNormal);
        return Math.Round(Math.Clamp(percentage, ExamResult.MinPercentage, ExamResult.MaxPercentage), 1);
    }
}
