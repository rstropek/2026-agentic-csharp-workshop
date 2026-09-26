using System.CommandLine;

namespace GraderCli.Commands;

/// <summary>
/// A command with examples in its help. Commands that call the API set their action with <see cref="SetApiAction"/>.
/// </summary>
internal sealed class GraderCommand(string name, string description, params IReadOnlyList<string> examples)
    : Command(name, description)
{
    public IReadOnlyList<string> Examples { get; } = examples;

    public void SetApiAction(Func<CommandInvocation, CancellationToken, Task> action) =>
        SetAction((parseResult, cancellationToken) => CommandInvocation.RunAsync(parseResult, action, cancellationToken));

    /// <summary>
    /// <c>--file</c>: the JSON document to send; stdin if missing.
    /// </summary>
    public static Option<string> CreateFileOption() => new("--file", "-f")
    {
        Description = "JSON file with the complete document (same shape as the API request). Default: stdin (\"-\").",
        HelpName = "path",
    };

    /// <summary>
    /// <c>--yes</c>: confirms a destructive command. The CLI never prompts.
    /// </summary>
    public static Option<bool> CreateYesOption() => new("--yes", "-y")
    {
        Description = "Confirm the destructive operation (required, the CLI never prompts).",
    };
}
