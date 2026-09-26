using System.CommandLine;
using System.CommandLine.Help;

namespace GraderCli.Commands;

/// <summary>
/// The <c>grader</c> command with the global options. Command actions get the <see cref="CliContext"/> and the global options
/// from it (<see cref="ParseResult.RootCommandResult"/>), so commands can be defined independently of the context.
/// </summary>
internal sealed class GraderRootCommand : RootCommand
{
    public const string ApiUrlVariable = "GRADER_API_URL";

    /// <summary>
    /// The API in development (<c>dotnet run --project GraderApi --launch-profile http</c>).
    /// </summary>
    public const string DefaultApiUrl = "http://localhost:5100";

    public GraderRootCommand(CliContext context)
        : base("Grader CLI: manage classes, students, exams and results of the Grader app. "
            + "One command per use case of the Grader Web API.")
    {
        Context = context;
        ApiUrl = new("--api-url")
        {
            Description = $"Base URL of the Grader API (without /api). Overrides the environment variable {ApiUrlVariable}.",
            HelpName = "url",
            Recursive = true,
            DefaultValueFactory = _ => context.GetEnvironmentVariable(ApiUrlVariable) is { Length: > 0 } url ? url : DefaultApiUrl,
        };
        Json = new("--json")
        {
            Description = "Print the API's JSON response instead of text, and errors as JSON on stderr.",
            Recursive = true,
        };
        Options.Add(ApiUrl);
        Options.Add(Json);

        var helpOption = Options.OfType<HelpOption>().Single();
        helpOption.Action = new HelpWithExamplesAction((HelpAction)helpOption.Action!);
    }

    public CliContext Context { get; }

    public Option<string> ApiUrl { get; }

    public Option<bool> Json { get; }

    /// <summary>
    /// Shown after the default help of the root command.
    /// </summary>
    public static string HelpDetails { get; } = $"""
        Output:
          Readable text on stdout. With --json: the API's JSON response unchanged (nothing for commands without response body).
          Errors go to stderr, incl. the API's error code (e.g. class-not-found), as JSON with --json.

        Input:
          Commands that save (create, save) take the complete document as JSON from --file or stdin, same shape as the
          API request. The output of "get --json" can be edited and saved back.

        Exit codes:
          0  success
          1  unexpected error (incl. server errors of the API)
          2  invalid command line or input, or destructive command without --yes (nothing was sent to the API)
          3  request rejected by the API (400, e.g. validation-failed or a violated business rule)
          4  not found (404)
          5  conflict with existing data (409)
          6  API not reachable

        Environment:
          {ApiUrlVariable}  Base URL of the Grader API (default: {DefaultApiUrl}); --api-url overrides it.

        Examples:
          grader school-years list
          grader classes list 3
          grader classes get 7 --json > class.json
          grader classes save 7 --file class.json
          grader exams get 12 --json --api-url http://localhost:5200
        """;
}
