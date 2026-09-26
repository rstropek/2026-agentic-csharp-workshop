namespace GraderCli;

// No top-level statements: their global Program class would clash with GraderApi's Program in GraderTests.
internal static class Program
{
    private static Task<int> Main(string[] args) => CliApp.RunAsync(args, CliContext.FromConsole());
}
