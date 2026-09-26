using System.CommandLine;
using GraderCli.ApiClient.Models;
using GraderCli.Commands;

namespace GraderCli.ClassManagement;

/// <summary>
/// Commands for the class management use cases of the API (tag <c>ClassManagement</c>).
/// </summary>
internal static class ClassManagementCommands
{
    public static Command CreateSchoolYearsCommand()
    {
        var list = new GraderCommand("list", "Lists the school years to pick from, newest first.",
            "grader school-years list",
            "grader school-years list --json");
        list.SetApiAction(async (call, ct) =>
        {
            var json = await call.GetJsonAsync(call.Client.Api.SchoolYears.ToGetRequestInformation(), ct);
            await call.WriteListAsync(json, SchoolYearSummary.CreateFromDiscriminatorValue, WriteSchoolYears, ct);
        });

        return new GraderCommand("school-years", "School years (read-only).") { list };
    }

    public static Command CreateClassesCommand()
    {
        var schoolYearId = new Argument<int>("school-year-id") { Description = "Id of the school year." };
        var classId = new Argument<int>("class-id") { Description = "Id of the class." };

        var list = new GraderCommand("list", "Lists the classes of a school year incl. their number of students, sorted by name.\n"
            + "Errors: school-year-not-found (404).",
            "grader classes list 3",
            "grader classes list 3 --json")
        {
            schoolYearId,
        };
        list.SetApiAction(async (call, ct) =>
        {
            var request = call.Client.Api.SchoolYears[call.GetValue(schoolYearId)].Classes.ToGetRequestInformation();
            await call.WriteListAsync(await call.GetJsonAsync(request, ct), ClassSummary.CreateFromDiscriminatorValue, WriteClasses, ct);
        });

        var get = new GraderCommand("get", "Opens a class incl. its students, sorted by last name and first name.\n"
            + "Errors: class-not-found (404).",
            "grader classes get 7",
            "grader classes get 7 --json > class.json")
        {
            classId,
        };
        get.SetApiAction(async (call, ct) =>
        {
            var request = call.Client.Api.Classes[call.GetValue(classId)].ToGetRequestInformation();
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ClassDetails.CreateFromDiscriminatorValue, WriteClass, ct);
        });

        var createFile = GraderCommand.CreateFileOption();
        var create = new GraderCommand("create", "Creates a class incl. its students in a school year. "
            + "Input: the class as JSON (SaveClassRequest); students have no id (null).\n"
            + "Errors: school-year-not-found (404), class-name-taken (409), unknown-student (400).",
            """grader classes create 3 --file class.json""",
            """echo '{"name": "1A", "students": [{"id": null, "firstName": "Ada", "lastName": "Lovelace"}]}' | grader classes create 3""")
        {
            schoolYearId,
            createFile,
        };
        create.SetApiAction(async (call, ct) =>
        {
            // Placeholder body, replaced by the document as read (see CommandInvocation)
            var request = call.Client.Api.SchoolYears[call.GetValue(schoolYearId)].Classes.ToPostRequestInformation(new());
            await call.SetDocumentAsync(request, call.GetValue(createFile), ct);
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ClassDetails.CreateFromDiscriminatorValue, WriteClass, ct);
        });

        var saveFile = GraderCommand.CreateFileOption();
        var save = new GraderCommand("save", "Saves a class incl. the complete list of its students: students without id (null) "
            + "are added, students with id are updated, students missing in the list are removed. "
            + "Input: the class as JSON (SaveClassRequest; the output of \"classes get --json\" works, too).\n"
            + "Errors: class-not-found (404), unknown-student and duplicate-student (400), "
            + "class-name-taken and student-has-results (409).",
            "grader classes get 7 --json > class.json   # edit class.json, then:",
            "grader classes save 7 --file class.json",
            """echo '{"name": "1A", "students": [{"id": 12, "firstName": "Ada", "lastName": "Lovelace"}]}' | grader classes save 7""")
        {
            classId,
            saveFile,
        };
        save.SetApiAction(async (call, ct) =>
        {
            // Placeholder body, replaced by the document as read (see CommandInvocation)
            var request = call.Client.Api.Classes[call.GetValue(classId)].ToPutRequestInformation(new());
            await call.SetDocumentAsync(request, call.GetValue(saveFile), ct);
            await call.WriteAsync(await call.GetJsonAsync(request, ct), ClassDetails.CreateFromDiscriminatorValue, WriteClass, ct);
        });

        var yes = GraderCommand.CreateYesOption();
        var delete = new GraderCommand("delete", "Deletes a class incl. its students.\n"
            + "Errors: class-not-found (404), class-has-exams (409).",
            "grader classes delete 7 --yes")
        {
            classId,
            yes,
        };
        delete.SetApiAction(async (call, ct) =>
        {
            var id = call.GetValue(classId);
            call.RequireConfirmation(yes, $"Deleting class {id} incl. its students");
            await call.SendAsync(call.Client.Api.Classes[id].ToDeleteRequestInformation(), ct);
            call.WriteMessage($"Deleted class {id}.");
        });

        return new GraderCommand("classes", "Classes incl. their students.") { list, get, create, save, delete };
    }

    private static void WriteSchoolYears(TextWriter output, IReadOnlyList<SchoolYearSummary> schoolYears) =>
        TextTable.Write(output, ["ID", "NAME"], schoolYears.Select(schoolYear => new object?[] { schoolYear.Id, schoolYear.Name }));

    private static void WriteClasses(TextWriter output, IReadOnlyList<ClassSummary> classes) =>
        TextTable.Write(output, ["ID", "NAME", "STUDENTS"],
            classes.Select(schoolClass => new object?[] { schoolClass.Id, schoolClass.Name, schoolClass.StudentCount }));

    private static void WriteClass(TextWriter output, ClassDetails schoolClass)
    {
        output.WriteLine($"Class {schoolClass.Name} (id {schoolClass.Id}), "
            + $"school year {schoolClass.SchoolYearName} (id {schoolClass.SchoolYearId})");
        output.WriteLine();
        TextTable.Write(output, ["ID", "LAST NAME", "FIRST NAME"],
            (schoolClass.Students ?? []).Select(student => new object?[] { student.Id, student.LastName, student.FirstName }));
    }
}
