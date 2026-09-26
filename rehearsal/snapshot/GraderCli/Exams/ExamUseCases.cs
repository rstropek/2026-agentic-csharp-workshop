using GraderCli.ApiClient.Models;
using GraderCli.Commands;
using GraderCli.UseCases;

namespace GraderCli.Exams;

/// <summary>
/// The exam use cases of the API (tag <c>Exams</c>).
/// </summary>
internal static class ExamUseCases
{
    private static readonly IdParameter classId = new("class-id", "Id of the class.");
    private static readonly IdParameter examId = new("exam-id", "Id of the exam.");

    public static UseCaseArea Courses { get; } = new("courses", "Courses (read-only).",
        new UseCase
        {
            Verb = "list",
            Description = "Lists the courses to pick from, sorted by name.",
            Examples = ["grader courses list", "grader courses list --json"],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, _) => client.Api.Courses.ToGetRequestInformation(),
            Response = UseCaseResponse.List(CourseSummary.CreateFromDiscriminatorValue, WriteCourses),
        });

    public static UseCaseArea Exams { get; } = new("exams", "Exams incl. their results.",
        new UseCase
        {
            Verb = "list",
            Description = "Lists the exams of a class incl. their course, sorted by date.\n"
                + "Errors: class-not-found (404).",
            Examples = ["grader exams list 7", "grader exams list 7 --json"],
            Ids = [classId],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, ids) => client.Api.Classes[ids[0]].Exams.ToGetRequestInformation(),
            Response = UseCaseResponse.List(ExamSummary.CreateFromDiscriminatorValue, WriteExams),
        },
        new UseCase
        {
            Verb = "get",
            Description = "Opens an exam incl. one result per student of the class, sorted by last name and first name. "
                + "The percentage is null for students without a result.\n"
                + "Errors: exam-not-found (404).",
            Examples = ["grader exams get 12", "grader exams get 12 --json > exam.json"],
            Ids = [examId],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, ids) => client.Api.Exams[ids[0]].ToGetRequestInformation(),
            Response = UseCaseResponse.Document(ExamDetails.CreateFromDiscriminatorValue, WriteExam),
        },
        new UseCase
        {
            Verb = "create",
            Description = "Creates an exam of a class incl. its results. Results are only allowed for students of the class. "
                + "Input: the exam as JSON (SaveExamRequest), date as yyyy-MM-dd, percentage 0 to 100.\n"
                + "Errors: class-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).",
            Examples =
            [
                "grader exams create 7 --file exam.json",
                """echo '{"examDate": "2025-10-15", "courseId": 4, "results": [{"studentId": 12, "percentage": 87.5}]}' | grader exams create 7""",
            ],
            Ids = [classId],
            DocumentSchema = "SaveExamRequest",
            Effect = UseCaseEffect.Create,
            CreateRequest = (client, ids) => client.Api.Classes[ids[0]].Exams.ToPostRequestInformation(new()),
            Response = UseCaseResponse.Document(ExamDetails.CreateFromDiscriminatorValue, WriteExam),
        },
        new UseCase
        {
            Verb = "save",
            Description = "Saves date, course and the complete list of results of an exam: results missing in the list or "
                + "with a null percentage are removed. The class of an exam cannot be changed. "
                + "Input: the exam as JSON (SaveExamRequest; the exam as returned by get works, too).\n"
                + "Errors: exam-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).",
            Examples =
            [
                "grader exams get 12 --json > exam.json   # edit exam.json, then:",
                "grader exams save 12 --file exam.json",
            ],
            Ids = [examId],
            DocumentSchema = "SaveExamRequest",
            Effect = UseCaseEffect.Replace,
            CreateRequest = (client, ids) => client.Api.Exams[ids[0]].ToPutRequestInformation(new()),
            Response = UseCaseResponse.Document(ExamDetails.CreateFromDiscriminatorValue, WriteExam),
        },
        new UseCase
        {
            Verb = "delete",
            Description = "Deletes an exam incl. its results.\n"
                + "Errors: exam-not-found (404).",
            Examples = ["grader exams delete 12 --yes"],
            Ids = [examId],
            Effect = UseCaseEffect.Delete,
            CreateRequest = (client, ids) => client.Api.Exams[ids[0]].ToDeleteRequestInformation(),
            Response = UseCaseResponse.None(ids => $"Deleted exam {ids[0]}."),
        });

    private static void WriteCourses(TextWriter output, IReadOnlyList<CourseSummary> courses) =>
        TextTable.Write(output, ["ID", "NAME"], courses.Select(course => new object?[] { course.Id, course.Name }));

    private static void WriteExams(TextWriter output, IReadOnlyList<ExamSummary> exams) =>
        TextTable.Write(output, ["ID", "DATE", "COURSE"], exams.Select(exam => new object?[] { exam.Id, exam.ExamDate, exam.CourseName }));

    private static void WriteExam(TextWriter output, ExamDetails exam)
    {
        output.WriteLine($"Exam {exam.Id}: {exam.CourseName} (course {exam.CourseId}) on {exam.ExamDate}");
        output.WriteLine($"Class {exam.ClassName} (id {exam.ClassId}), school year {exam.SchoolYearName} (id {exam.SchoolYearId})");
        output.WriteLine();
        TextTable.Write(output, ["STUDENT ID", "LAST NAME", "FIRST NAME", "PERCENTAGE"],
            (exam.Results ?? []).Select(result => new object?[] { result.StudentId, result.LastName, result.FirstName, result.Percentage }));
    }
}
