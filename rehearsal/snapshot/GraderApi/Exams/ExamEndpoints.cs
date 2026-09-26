using System.ComponentModel;
using GraderLogic.Exams;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GraderApi.Exams;

public static class ExamEndpoints
{
    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("").WithTags("Exams");

        group.MapGet("/courses", GetCourses)
            .WithName("GetCourses")
            .WithSummary("Lists the courses to pick from")
            .WithDescription("Returns all courses, sorted by name.");

        group.MapGet("/classes/{classId:int}/exams", GetExams)
            .WithName("GetExams")
            .WithSummary("Lists the exams of a class")
            .WithDescription("Returns the exams of a class incl. their course, sorted by date.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/exams/{examId:int}", GetExam)
            .WithName("GetExam")
            .WithSummary("Opens an exam incl. its results")
            .WithDescription("Returns an exam incl. one result per student of the class, sorted by last name and first name. "
                + "The percentage is null for students without a result.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/classes/{classId:int}/exams", CreateExam)
            .WithName("CreateExam")
            .WithSummary("Creates an exam incl. its results")
            .WithDescription("Creates an exam of a class. Results are only allowed for students of the class. "
                + "Errors: class-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/exams/{examId:int}", SaveExam)
            .WithName("SaveExam")
            .WithSummary("Saves an exam incl. its results")
            .WithDescription("Saves date, course and the complete list of results of an exam: results missing in the list "
                + "or with a null percentage are removed. The class of an exam cannot be changed. "
                + "Errors: exam-not-found (404), unknown-course, student-not-in-class and duplicate-student (400), exam-exists (409).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/exams/{examId:int}", DeleteExam)
            .WithName("DeleteExam")
            .WithSummary("Deletes an exam incl. its results")
            .WithDescription("Deletes an exam incl. its results. Errors: exam-not-found (404).")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<Ok<IReadOnlyList<CourseSummary>>> GetCourses(IExamService service, CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetCoursesAsync(cancellationToken));

    private static async Task<Ok<IReadOnlyList<ExamSummary>>> GetExams(
        [Description("Id of the class.")] int classId,
        IExamService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetExamsAsync(classId, cancellationToken));

    private static async Task<Ok<ExamDetails>> GetExam(
        [Description("Id of the exam.")] int examId,
        IExamService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetExamAsync(examId, cancellationToken));

    private static async Task<Created<ExamDetails>> CreateExam(
        [Description("Id of the class.")] int classId,
        SaveExamRequest request,
        IExamService service,
        CancellationToken cancellationToken)
    {
        var exam = await service.CreateExamAsync(classId, request, cancellationToken);
        return TypedResults.Created($"/api/exams/{exam.Id}", exam);
    }

    private static async Task<Ok<ExamDetails>> SaveExam(
        [Description("Id of the exam.")] int examId,
        SaveExamRequest request,
        IExamService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.SaveExamAsync(examId, request, cancellationToken));

    private static async Task<NoContent> DeleteExam(
        [Description("Id of the exam.")] int examId,
        IExamService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteExamAsync(examId, cancellationToken);
        return TypedResults.NoContent();
    }
}
