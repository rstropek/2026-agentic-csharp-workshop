using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using GraderCli.Errors;
using GraderCli.UseCases;

namespace GraderCli.Commands;

/// <summary>
/// One run of a command that calls the API: parsed command line, input and output. The API call itself is shared with
/// the MCP tools (<see cref="ApiCaller"/>).
/// </summary>
internal sealed class CommandInvocation
{
    private static readonly JsonSerializerOptions indentedJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly ParseResult parseResult;
    private readonly bool jsonOutput;

    private CommandInvocation(ParseResult parseResult, CliContext context, string apiUrl, bool jsonOutput)
    {
        this.parseResult = parseResult;
        Context = context;
        ApiUrl = apiUrl;
        this.jsonOutput = jsonOutput;
    }

    /// <summary>
    /// Runs a command action and turns every error into a message on stderr and an exit code (see <see cref="CliError"/>).
    /// </summary>
    public static async Task<int> RunAsync(
        ParseResult parseResult, Func<CommandInvocation, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var root = (GraderRootCommand)parseResult.RootCommandResult.Command;
        var apiUrl = parseResult.GetValue(root.ApiUrl) ?? GraderRootCommand.DefaultApiUrl;
        var jsonOutput = parseResult.GetValue(root.Json);
        try
        {
            await action(new CommandInvocation(parseResult, root.Context, apiUrl, jsonOutput), cancellationToken);
            return ExitCodes.Success;
        }
        // Every error must end up on stderr with an error code and a meaningful exit code.
        catch (Exception exception)
        {
            var error = CliError.FromException(exception, apiUrl);
            error.WriteTo(root.Context.Error, jsonOutput);
            return error.ExitCode;
        }
    }

    public CliContext Context { get; }

    public string ApiUrl { get; }

    public T GetValue<T>(Argument<T> argument) => parseResult.GetRequiredValue(argument);

    public T? GetValue<T>(Option<T> option) => parseResult.GetValue(option);

    /// <summary>
    /// Fails unless the destructive <paramref name="command"/> (e.g. <c>grader classes delete</c>) was confirmed with <c>--yes</c>.
    /// </summary>
    public void RequireConfirmation(Option<bool> yesOption, string command)
    {
        if (!parseResult.GetValue(yesOption))
        {
            throw new CliException("confirmation-required", $"'{command}' cannot be undone. Add --yes to confirm.");
        }
    }

    /// <summary>
    /// Reads the JSON document from the file (<c>null</c> or <c>-</c>: stdin) and checks that it is a JSON object.
    /// </summary>
    public async Task<string> ReadDocumentAsync(string? path, CancellationToken cancellationToken)
    {
        string json;
        if (path is null or "-")
        {
            if (!Context.IsInputRedirected)
            {
                throw new CliException("input-missing", "No JSON document: pass --file <path> or pipe the document to stdin.");
            }

            json = await Context.Input.ReadToEndAsync(cancellationToken);
        }
        else
        {
            json = await InputDocument.ReadFileAsync(path, cancellationToken);
        }

        InputDocument.Validate(json);
        return json;
    }

    public Task<string?> CallAsync(UseCase useCase, IReadOnlyList<int> ids, string? document, CancellationToken cancellationToken) =>
        ApiCaller.CallAsync(useCase, ids, document, ApiUrl, Context.CreateHttpClient, cancellationToken);

    /// <summary>
    /// Writes a response: with <c>--json</c> the API's JSON (nothing for responses without body, like the API),
    /// otherwise as text.
    /// </summary>
    public async Task WriteAsync(UseCaseResponse response, string? json, IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        if (!jsonOutput)
        {
            await response.WriteTextAsync(Context.Output, json, ids, cancellationToken);
        }
        else if (json is not null)
        {
            using var document = JsonDocument.Parse(json);
            Context.Output.WriteLine(JsonSerializer.Serialize(document.RootElement, indentedJson));
        }
    }
}
