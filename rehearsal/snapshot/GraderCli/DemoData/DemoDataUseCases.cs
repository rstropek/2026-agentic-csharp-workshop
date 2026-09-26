using GraderCli.UseCases;

namespace GraderCli.DemoData;

/// <summary>
/// The demo data use case of the API (tag <c>DemoData</c>).
/// </summary>
internal static class DemoDataUseCases
{
    public static UseCaseArea DemoData { get; } = new("demo-data", "Demo data.",
        new UseCase
        {
            Verb = "replace",
            Description = "Deletes all data and fills the database with generated demo data "
                + "(school years, classes, students, courses, exams and results). All existing data is lost.",
            Examples = ["grader demo-data replace --yes"],
            Effect = UseCaseEffect.Delete,
            CreateRequest = (client, _) => client.Api.DemoData.ToPostRequestInformation(),
            Response = UseCaseResponse.None(_ => "Replaced all data with demo data."),
        });
}
