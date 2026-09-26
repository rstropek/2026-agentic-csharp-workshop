using System.CommandLine;
using System.CommandLine.Invocation;
using GraderCli.Commands;
using GraderCli.Errors;
using GraderCli.Mcp;
using GraderCli.UseCases;

namespace GraderCli;

/// <summary>
/// Entry point of the CLI: one command per use case of the Web API, and the MCP server (see tech-docs/cli.md).
/// </summary>
public static class CliApp
{
    public static async Task<int> RunAsync(string[] args, CliContext context, CancellationToken cancellationToken = default)
    {
        var root = new GraderRootCommand(context);
        foreach (var area in UseCaseCatalog.Areas)
        {
            root.Subcommands.Add(UseCaseCommands.CreateAreaCommand(area));
        }

        root.Subcommands.Add(McpServerCommand.Create());

        var parseResult = root.Parse(args);
        var exitCode = await parseResult.InvokeAsync(
            new InvocationConfiguration { Output = context.Output, Error = context.Error }, cancellationToken);

        // System.CommandLine returns 1 for parse errors, but the CLI reserves 1 for unexpected errors. Help requested
        // despite parse errors (e.g. "classes get --help" without id) is no error.
        return parseResult.Action is ParseErrorAction ? ExitCodes.UsageError : exitCode;
    }
}
