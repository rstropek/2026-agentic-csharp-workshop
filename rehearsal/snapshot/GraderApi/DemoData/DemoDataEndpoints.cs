using GraderLogic.DemoData;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GraderApi.DemoData;

public static class DemoDataEndpoints
{
    public static IEndpointRouteBuilder MapDemoDataEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/demo-data").WithTags("DemoData");

        group.MapPost("", ReplaceWithDemoData)
            .WithName("ReplaceWithDemoData")
            .WithSummary("Replaces all data with demo data")
            .WithDescription("Deletes all data and fills the database with generated demo data (school years, classes, "
                + "students, courses, exams and results). All existing data is lost.");

        return routes;
    }

    private static async Task<NoContent> ReplaceWithDemoData(IDemoDataService service, CancellationToken cancellationToken)
    {
        await service.ReplaceWithDemoDataAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
