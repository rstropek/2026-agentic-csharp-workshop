using System.ComponentModel;
using GraderApi.Tools;
using GraderApi.Validation;
using GraderLogic.ClassManagement;

namespace GraderApi.ClassManagement;

/// <summary>
/// The class management use cases as tools for AI agents, part of the <see cref="ToolCatalog"/> (see tech-docs/mcp.md).
/// The descriptions tell the model what they do and which error codes they throw.
/// </summary>
public sealed class ClassManagementTools(IClassManagementService service, RequestValidator validator)
{
    [Tool("school_years_list", ToolEffect.Read)]
    [Description("Lists the school years to pick from, newest first.")]
    public Task<IReadOnlyList<SchoolYearSummary>> GetSchoolYearsAsync(CancellationToken cancellationToken)
        => service.GetSchoolYearsAsync(cancellationToken);

    [Tool("classes_list", ToolEffect.Read)]
    [Description("Lists the classes of a school year incl. their number of students, sorted by name.\n"
        + "Errors: school-year-not-found (404).")]
    public Task<IReadOnlyList<ClassSummary>> GetClassesAsync(
        [Description("Id of the school year.")] int schoolYearId, CancellationToken cancellationToken)
        => service.GetClassesAsync(schoolYearId, cancellationToken);

    [Tool("classes_get", ToolEffect.Read)]
    [Description("Opens a class incl. its students, sorted by last name and first name.\n"
        + "Errors: class-not-found (404).")]
    public Task<ClassDetails> GetClassAsync([Description("Id of the class.")] int classId, CancellationToken cancellationToken)
        => service.GetClassAsync(classId, cancellationToken);

    [Tool("classes_create", ToolEffect.Create)]
    [Description("Creates a class incl. its students in a school year. "
        + "Input: the class as JSON (SaveClassRequest); students have no id (null).\n"
        + "Errors: school-year-not-found (404), class-name-taken (409), unknown-student (400).")]
    public async Task<ClassDetails> CreateClassAsync(
        [Description("Id of the school year.")] int schoolYearId, SaveClassRequest document, CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(document, cancellationToken);
        return await service.CreateClassAsync(schoolYearId, document, cancellationToken);
    }

    [Tool("classes_save", ToolEffect.Replace)]
    [Description("Saves a class incl. the complete list of its students: students without id (null) are added, "
        + "students with id are updated, students missing in the list are removed. "
        + "Input: the class as JSON (SaveClassRequest; the class as returned by get works, too).\n"
        + "Errors: class-not-found (404), unknown-student and duplicate-student (400), "
        + "class-name-taken and student-has-results (409).")]
    public async Task<ClassDetails> SaveClassAsync(
        [Description("Id of the class.")] int classId, SaveClassRequest document, CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(document, cancellationToken);
        return await service.SaveClassAsync(classId, document, cancellationToken);
    }

    [Tool("classes_delete", ToolEffect.Delete)]
    [Description("Deletes a class incl. its students.\n"
        + "Errors: class-not-found (404), class-has-exams (409).")]
    public async Task<string> DeleteClassAsync([Description("Id of the class.")] int classId, CancellationToken cancellationToken)
    {
        await service.DeleteClassAsync(classId, cancellationToken);
        return $"Deleted class {classId}.";
    }
}
