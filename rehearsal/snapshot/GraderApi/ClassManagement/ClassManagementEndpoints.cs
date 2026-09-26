using System.ComponentModel;
using GraderLogic.ClassManagement;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GraderApi.ClassManagement;

public static class ClassManagementEndpoints
{
    public static IEndpointRouteBuilder MapClassManagementEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("").WithTags("ClassManagement");

        group.MapGet("/school-years", GetSchoolYears)
            .WithName("GetSchoolYears")
            .WithSummary("Lists the school years to pick from")
            .WithDescription("Returns all school years, newest first.");

        group.MapGet("/school-years/{schoolYearId:int}/classes", GetClasses)
            .WithName("GetClasses")
            .WithSummary("Lists the classes of a school year")
            .WithDescription("Returns the classes of a school year incl. their number of students, sorted by name.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/classes/{classId:int}", GetClass)
            .WithName("GetClass")
            .WithSummary("Opens a class incl. its students")
            .WithDescription("Returns a class incl. its students, sorted by last name and first name.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/school-years/{schoolYearId:int}/classes", CreateClass)
            .WithName("CreateClass")
            .WithSummary("Creates a class incl. its students")
            .WithDescription("Creates a class in a school year. Students must not have an id. "
                + "Errors: school-year-not-found (404), class-name-taken (409), unknown-student (400).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/classes/{classId:int}", SaveClass)
            .WithName("SaveClass")
            .WithSummary("Saves a class incl. its students")
            .WithDescription("Saves a class incl. the complete list of its students: students without id are added, "
                + "students with id are updated, students missing in the list are removed. "
                + "Errors: class-not-found (404), unknown-student and duplicate-student (400), "
                + "class-name-taken and student-has-results (409).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/classes/{classId:int}", DeleteClass)
            .WithName("DeleteClass")
            .WithSummary("Deletes a class incl. its students")
            .WithDescription("Deletes a class incl. its students. Errors: class-not-found (404), class-has-exams (409).")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }

    private static async Task<Ok<IReadOnlyList<SchoolYearSummary>>> GetSchoolYears(
        IClassManagementService service, CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetSchoolYearsAsync(cancellationToken));

    private static async Task<Ok<IReadOnlyList<ClassSummary>>> GetClasses(
        [Description("Id of the school year.")] int schoolYearId,
        IClassManagementService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetClassesAsync(schoolYearId, cancellationToken));

    private static async Task<Ok<ClassDetails>> GetClass(
        [Description("Id of the class.")] int classId,
        IClassManagementService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.GetClassAsync(classId, cancellationToken));

    private static async Task<Created<ClassDetails>> CreateClass(
        [Description("Id of the school year.")] int schoolYearId,
        SaveClassRequest request,
        IClassManagementService service,
        CancellationToken cancellationToken)
    {
        var schoolClass = await service.CreateClassAsync(schoolYearId, request, cancellationToken);
        return TypedResults.Created($"/api/classes/{schoolClass.Id}", schoolClass);
    }

    private static async Task<Ok<ClassDetails>> SaveClass(
        [Description("Id of the class.")] int classId,
        SaveClassRequest request,
        IClassManagementService service,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await service.SaveClassAsync(classId, request, cancellationToken));

    private static async Task<NoContent> DeleteClass(
        [Description("Id of the class.")] int classId,
        IClassManagementService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteClassAsync(classId, cancellationToken);
        return TypedResults.NoContent();
    }
}
