using System.CommandLine;
using GraderCli.ApiClient.Models;
using GraderCli.Commands;

namespace GraderCli.Exams;

/// <summary>
/// Commands for the exam use cases of the API (tag <c>Exams</c>).
/// </summary>
internal static class ExamCommands
{
    public static Command CreateCoursesCommand()
    {
        var list = new GraderCommand("list", "Lists the courses to pick from, sorted by name.",
            "grader courses list",
            "grader courses list --json");
        list.SetApiAction(async (call, ct) =>
        {
            var json = await call.GetJsonAsync(call.Client.Api.Courses.ToGetRequestInformation(), ct);
            await call.WriteListAsync(json, CourseSummary.CreateFromDiscriminatorValue, WriteCourses, ct);
        });

        return new GraderCommand("courses", "Courses (read-only).") { list };
    }

    public static Command CreateExamsCommand()
    {
        var classId = new Argument<int>("class-id") { Description = "Id of the class." };
        var examId = new Argument<int>("exam-id") { Description = "Id of the exam." };

        var list = new GraderCommand("list", "Lists the exams of a class incl. their course, sorted by date.\n"
            + "Errors: class-not-found (404).",
            "grader exams list 7",
            "grader exams list 7 --json")
        {
            classId,
        };
        list.SetApiAction(async (call, ct) =>
        {
            var request = call.Client.Api.Classes[call.GetValue(classId)].Exams.ToGetRequestInformation();
            await call.WriteListAsync(await call.GetJsonAsync(request, ct), ExamSummary.CreateFromDiscriminatorValue, WriteExams, ct);
        });

        var get = new GraderCommand("get", "Opens an exam incl. one result per student of the class, sorted by last name and "
            + "first name. The percentage is null for students without a result.\n"
            + "Errors: exam-not-found (404).",
            "grader exams get 12",
            "grader exams get 12 --json > exam.json")
        {
            examId,
        };
        get.SetApiAction(async (call, ct) =>
        {
            var request = call.Client.Api.Exams[call.GetValue(examId)].ToGetRequestInformation();
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ExamDetails.CreateFromDiscriminatorValue, WriteExam, ct);
        });

        var createFile = GraderCommand.CreateFileOption();
        var create = new GraderCommand("create", "Creates an exam of a class incl. its results. Results are only allowed for "
            + "students of the class. Input: the exam as JSON (SaveExamRequest), date as yyyy-MM-dd, percentage 0 to 100.\n"
            + "Errors: class-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).",
            "grader exams create 7 --file exam.json",
            """echo '{"examDate": "2025-10-15", "courseId": 4, "results": [{"studentId": 12, "percentage": 87.5}]}' | grader exams create 7""")
        {
            classId,
            createFile,
        };
        create.SetApiAction(async (call, ct) =>
        {
            // Placeholder body, replaced by the document as read (see CommandInvocation)
            var request = call.Client.Api.Classes[call.GetValue(classId)].Exams.ToPostRequestInformation(new());
            await call.SetDocumentAsync(request, call.GetValue(createFile), ct);
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ExamDetails.CreateFromDiscriminatorValue, WriteExam, ct);
        });

        var saveFile = GraderCommand.CreateFileOption();
        var save = new GraderCommand("save", "Saves date, course and the complete list of results of an exam: results missing "
            + "in the list or with a null percentage are removed. The class of an exam cannot be changed. "
            + "Input: the exam as JSON (SaveExamRequest; the output of \"exams get --json\" works, too).\n"
            + "Errors: exam-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).",
            "grader exams get 12 --json > exam.json   # edit exam.json, then:",
            "grader exams save 12 --file exam.json")
        {
            examId,
            saveFile,
        };
        save.SetApiAction(async (call, ct) =>
        {
            // Placeholder body, replaced by the document as read (see CommandInvocation)
            var request = call.Client.Api.Exams[call.GetValue(examId)].ToPutRequestInformation(new());
            await call.SetDocumentAsync(request, call.GetValue(saveFile), ct);
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ExamDetails.CreateFromDiscriminatorValue, WriteExam, ct);
        });

        var yes = GraderCommand.CreateYesOption();
        var delete = new GraderCommand("delete", "Deletes an exam incl. its results.\n"
            + "Errors: exam-not-found (404).",
            "grader exams delete 12 --yes")
        {
            examId,
            yes,
        };
        delete.SetApiAction(async (call, ct) =>
        {
            var id = call.GetValue(examId);
            call.RequireConfirmation(yes, $"Deleting exam {id} incl. its results");
            await call.SendAsync(call.Client.Api.Exams[id].ToDeleteRequestInformation(), ct);
            call.WriteMessage($"Deleted exam {id}.");
        });

        return new GraderCommand("exams", "Exams incl. their results.") { list, get, create, save, delete };
    }

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
