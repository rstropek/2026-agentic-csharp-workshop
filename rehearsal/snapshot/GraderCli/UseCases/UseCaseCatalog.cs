using GraderCli.ClassManagement;
using GraderCli.DemoData;
using GraderCli.Exams;

namespace GraderCli.UseCases;

/// <summary>
/// All use cases of the API, in the order of the CLI help and the MCP tool list.
/// </summary>
internal static class UseCaseCatalog
{
    public static IReadOnlyList<UseCaseArea> Areas { get; } =
    [
        ClassManagementUseCases.SchoolYears,
        ClassManagementUseCases.Classes,
        ExamUseCases.Courses,
        ExamUseCases.Exams,
        DemoDataUseCases.DemoData,
    ];
}
