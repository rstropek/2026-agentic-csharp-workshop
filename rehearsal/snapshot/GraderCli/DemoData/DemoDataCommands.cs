using System.CommandLine;
using GraderCli.Commands;

namespace GraderCli.DemoData;

/// <summary>
/// Commands for the demo data use case of the API (tag <c>DemoData</c>).
/// </summary>
internal static class DemoDataCommands
{
    public static Command CreateDemoDataCommand()
    {
        var yes = GraderCommand.CreateYesOption();
        var replace = new GraderCommand("replace", "Deletes all data and fills the database with generated demo data "
            + "(school years, classes, students, courses, exams and results). All existing data is lost.",
            "grader demo-data replace --yes")
        {
            yes,
        };
        replace.SetApiAction(async (call, ct) =>
        {
            call.RequireConfirmation(yes, "Replacing all data with demo data");
            await call.SendAsync(call.Client.Api.DemoData.ToPostRequestInformation(), ct);
            call.WriteMessage("Replaced all data with demo data.");
        });

        return new GraderCommand("demo-data", "Demo data.") { replace };
    }
}
