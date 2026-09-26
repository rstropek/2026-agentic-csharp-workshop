using System.ComponentModel;
using GraderApi.Tools;
using GraderApi.Validation;
using GraderLogic.Exams;

namespace GraderApi.Exams;

/// <summary>
/// The exam use cases as tools for AI agents, part of the <see cref="ToolCatalog"/> (see tech-docs/mcp.md). The
/// descriptions tell the model what they do and which error codes they throw.
/// </summary>
public sealed class ExamTools(IExamService service, RequestValidator validator)
{
    [Tool("courses_list", ToolEffect.Read)]
    [Description("Lists the courses to pick from, sorted by name.")]
    public Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken cancellationToken)
        => service.GetCoursesAsync(cancellationToken);

    [Tool("exams_list", ToolEffect.Read)]
    [Description("Lists the exams of a class incl. their course, sorted by date.\n"
        + "Errors: class-not-found (404).")]
    public Task<IReadOnlyList<ExamSummary>> GetExamsAsync([Description("Id of the class.")] int classId, CancellationToken cancellationToken)
        => service.GetExamsAsync(classId, cancellationToken);

    [Tool("exams_get", ToolEffect.Read)]
    [Description("Opens an exam incl. one result per student of the class, sorted by last name and first name. "
        + "The percentage is null for students without a result.\n"
        + "Errors: exam-not-found (404).")]
    public Task<ExamDetails> GetExamAsync([Description("Id of the exam.")] int examId, CancellationToken cancellationToken)
        => service.GetExamAsync(examId, cancellationToken);

    [Tool("exams_create", ToolEffect.Create)]
    [Description("Creates an exam of a class incl. its results. Results are only allowed for students of the class. "
        + "Input: the exam as JSON (SaveExamRequest), date as yyyy-MM-dd, percentage 0 to 100.\n"
        + "Errors: class-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).")]
    public async Task<ExamDetails> CreateExamAsync(
        [Description("Id of the class.")] int classId, SaveExamRequest document, CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(document, cancellationToken);
        return await service.CreateExamAsync(classId, document, cancellationToken);
    }

    [Tool("exams_save", ToolEffect.Replace)]
    [Description("Saves date, course and the complete list of results of an exam: results missing in the list or "
        + "with a null percentage are removed. The class of an exam cannot be changed. "
        + "Input: the exam as JSON (SaveExamRequest; the exam as returned by get works, too).\n"
        + "Errors: exam-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).")]
    public async Task<ExamDetails> SaveExamAsync(
        [Description("Id of the exam.")] int examId, SaveExamRequest document, CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(document, cancellationToken);
        return await service.SaveExamAsync(examId, document, cancellationToken);
    }

    [Tool("exams_delete", ToolEffect.Delete)]
    [Description("Deletes an exam incl. its results.\n"
        + "Errors: exam-not-found (404).")]
    public async Task<string> DeleteExamAsync([Description("Id of the exam.")] int examId, CancellationToken cancellationToken)
    {
        await service.DeleteExamAsync(examId, cancellationToken);
        return $"Deleted exam {examId}.";
    }
}
