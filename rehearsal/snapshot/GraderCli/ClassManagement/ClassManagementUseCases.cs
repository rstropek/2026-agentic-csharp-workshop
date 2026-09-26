using GraderCli.ApiClient.Models;
using GraderCli.Commands;
using GraderCli.UseCases;

namespace GraderCli.ClassManagement;

/// <summary>
/// The class management use cases of the API (tag <c>ClassManagement</c>).
/// </summary>
internal static class ClassManagementUseCases
{
    private static readonly IdParameter schoolYearId = new("school-year-id", "Id of the school year.");
    private static readonly IdParameter classId = new("class-id", "Id of the class.");

    public static UseCaseArea SchoolYears { get; } = new("school-years", "School years (read-only).",
        new UseCase
        {
            Verb = "list",
            Description = "Lists the school years to pick from, newest first.",
            Examples = ["grader school-years list", "grader school-years list --json"],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, _) => client.Api.SchoolYears.ToGetRequestInformation(),
            Response = UseCaseResponse.List(SchoolYearSummary.CreateFromDiscriminatorValue, WriteSchoolYears),
        });

    public static UseCaseArea Classes { get; } = new("classes", "Classes incl. their students.",
        new UseCase
        {
            Verb = "list",
            Description = "Lists the classes of a school year incl. their number of students, sorted by name.\n"
                + "Errors: school-year-not-found (404).",
            Examples = ["grader classes list 3", "grader classes list 3 --json"],
            Ids = [schoolYearId],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, ids) => client.Api.SchoolYears[ids[0]].Classes.ToGetRequestInformation(),
            Response = UseCaseResponse.List(ClassSummary.CreateFromDiscriminatorValue, WriteClasses),
        },
        new UseCase
        {
            Verb = "get",
            Description = "Opens a class incl. its students, sorted by last name and first name.\n"
                + "Errors: class-not-found (404).",
            Examples = ["grader classes get 7", "grader classes get 7 --json > class.json"],
            Ids = [classId],
            Effect = UseCaseEffect.Read,
            CreateRequest = (client, ids) => client.Api.Classes[ids[0]].ToGetRequestInformation(),
            Response = UseCaseResponse.Document(ClassDetails.CreateFromDiscriminatorValue, WriteClass),
        },
        new UseCase
        {
            Verb = "create",
            Description = "Creates a class incl. its students in a school year. "
                + "Input: the class as JSON (SaveClassRequest); students have no id (null).\n"
                + "Errors: school-year-not-found (404), class-name-taken (409), unknown-student (400).",
            Examples =
            [
                "grader classes create 3 --file class.json",
                """echo '{"name": "1A", "students": [{"id": null, "firstName": "Ada", "lastName": "Lovelace"}]}' | grader classes create 3""",
            ],
            Ids = [schoolYearId],
            DocumentSchema = "SaveClassRequest",
            Effect = UseCaseEffect.Create,
            CreateRequest = (client, ids) => client.Api.SchoolYears[ids[0]].Classes.ToPostRequestInformation(new()),
            Response = UseCaseResponse.Document(ClassDetails.CreateFromDiscriminatorValue, WriteClass),
        },
        new UseCase
        {
            Verb = "save",
            Description = "Saves a class incl. the complete list of its students: students without id (null) are added, "
                + "students with id are updated, students missing in the list are removed. "
                + "Input: the class as JSON (SaveClassRequest; the class as returned by get works, too).\n"
                + "Errors: class-not-found (404), unknown-student and duplicate-student (400), "
                + "class-name-taken and student-has-results (409).",
            Examples =
            [
                "grader classes get 7 --json > class.json   # edit class.json, then:",
                "grader classes save 7 --file class.json",
                """echo '{"name": "1A", "students": [{"id": 12, "firstName": "Ada", "lastName": "Lovelace"}]}' | grader classes save 7""",
            ],
            Ids = [classId],
            DocumentSchema = "SaveClassRequest",
            Effect = UseCaseEffect.Replace,
            CreateRequest = (client, ids) => client.Api.Classes[ids[0]].ToPutRequestInformation(new()),
            Response = UseCaseResponse.Document(ClassDetails.CreateFromDiscriminatorValue, WriteClass),
        },
        new UseCase
        {
            Verb = "delete",
            Description = "Deletes a class incl. its students.\n"
                + "Errors: class-not-found (404), class-has-exams (409).",
            Examples = ["grader classes delete 7 --yes"],
            Ids = [classId],
            Effect = UseCaseEffect.Delete,
            CreateRequest = (client, ids) => client.Api.Classes[ids[0]].ToDeleteRequestInformation(),
            Response = UseCaseResponse.None(ids => $"Deleted class {ids[0]}."),
        });

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
