using System.ComponentModel;
using GraderApi.Tools;
using GraderLogic.DemoData;

namespace GraderApi.DemoData;

/// <summary>
/// The demo data use case as tool for AI agents, part of the <see cref="ToolCatalog"/> (see tech-docs/mcp.md).
/// </summary>
public sealed class DemoDataTools(IDemoDataService service)
{
    [Tool("demo_data_replace", ToolEffect.Delete)]
    [Description("Deletes all data and fills the database with generated demo data "
        + "(school years, classes, students, courses, exams and results). All existing data is lost.")]
    public async Task<string> ReplaceWithDemoDataAsync(CancellationToken cancellationToken)
    {
        await service.ReplaceWithDemoDataAsync(cancellationToken);
        return "Replaced all data with demo data.";
    }
}
