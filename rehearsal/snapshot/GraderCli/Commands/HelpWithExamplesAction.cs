using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace GraderCli.Commands;

/// <summary>
/// The default help, followed by the examples of a <see cref="GraderCommand"/> or the details of the root command
/// (output, exit codes, environment).
/// </summary>
internal sealed class HelpWithExamplesAction(HelpAction defaultHelp) : SynchronousCommandLineAction
{
    public override int Invoke(ParseResult parseResult)
    {
        var exitCode = defaultHelp.Invoke(parseResult);
        var output = parseResult.InvocationConfiguration.Output;
        switch (parseResult.CommandResult.Command)
        {
            case GraderRootCommand:
                output.WriteLine(GraderRootCommand.HelpDetails);
                output.WriteLine();
                break;
            case GraderCommand { Examples.Count: > 0 } command:
                output.WriteLine("Examples:");
                foreach (var example in command.Examples)
                {
                    output.WriteLine($"  {example}");
                }

                output.WriteLine();
                break;
        }

        return exitCode;
    }
}
