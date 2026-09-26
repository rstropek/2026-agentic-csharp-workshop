using System.CommandLine;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using GraderCli.ApiClient;
using GraderCli.ApiClient.Models;
using GraderCli.Errors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace GraderCli.Commands;

/// <summary>
/// One run of a command that calls the API: parsed command line, API client, input and output.
/// </summary>
/// <remarks>
/// Commands build requests with the generated Kiota request builders, but send request documents and print responses
/// as JSON unchanged instead of using Kiota's serializer: it never writes <c>null</c>, while the API requires explicit
/// nulls (e.g. the id of a new student) and returns them (e.g. a missing result). See tech-docs/cli.md.
/// </remarks>
internal sealed class CommandInvocation
{
    // Every error response is read as (validation) problem details, a superset of problem details. Unlike the
    // generated methods' error mappings, this includes status codes an endpoint does not declare (e.g. 500).
    private static readonly Dictionary<string, ParsableFactory<IParsable>> errorMapping = new()
    {
        ["XXX"] = HttpValidationProblemDetails.CreateFromDiscriminatorValue,
    };

    private static readonly JsonSerializerOptions indentedJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly ParseResult parseResult;
    private readonly CliContext context;
    private readonly bool jsonOutput;
    private readonly IRequestAdapter requestAdapter;

    private CommandInvocation(ParseResult parseResult, CliContext context, bool jsonOutput, IRequestAdapter requestAdapter)
    {
        this.parseResult = parseResult;
        this.context = context;
        this.jsonOutput = jsonOutput;
        this.requestAdapter = requestAdapter;
        Client = new GraderApiClient(requestAdapter);
    }

    public GraderApiClient Client { get; }

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
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri) || apiUri.Scheme is not ("http" or "https"))
            {
                throw new CliException("invalid-api-url", $"The API URL '{apiUrl}' is not an absolute http(s) URL.");
            }

            using var httpClient = root.Context.CreateHttpClient();
            using var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
            {
                BaseUrl = apiUrl,
            };
            var invocation = new CommandInvocation(parseResult, root.Context, jsonOutput, requestAdapter);
            await action(invocation, cancellationToken);
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

    public T GetValue<T>(Argument<T> argument) => parseResult.GetRequiredValue(argument);

    public T? GetValue<T>(Option<T> option) => parseResult.GetValue(option);

    /// <summary>
    /// Fails unless the destructive command was confirmed with <c>--yes</c>.
    /// </summary>
    public void RequireConfirmation(Option<bool> yesOption, string operation)
    {
        if (!parseResult.GetValue(yesOption))
        {
            throw new CliException("confirmation-required", $"{operation} cannot be undone. Add --yes to confirm.");
        }
    }

    /// <summary>
    /// Sets the body of a request to the JSON document from the file (<c>null</c> or <c>-</c>: stdin), unchanged.
    /// </summary>
    public async Task SetDocumentAsync(RequestInformation request, string? path, CancellationToken cancellationToken)
    {
        string json;
        if (path is null or "-")
        {
            if (!context.IsInputRedirected)
            {
                throw new CliException("input-missing", "No JSON document: pass --file <path> or pipe the document to stdin.");
            }

            json = await context.Input.ReadToEndAsync(cancellationToken);
        }
        else
        {
            try
            {
                json = await File.ReadAllTextAsync(path, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new CliException("input-unreadable", $"Cannot read the file '{path}': {exception.Message}");
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new CliException("input-missing", "The JSON document is empty.");
        }

        EnsureJsonObject(json);
        request.SetStreamContent(new MemoryStream(Encoding.UTF8.GetBytes(json)), "application/json");
    }

    /// <summary>
    /// Sends a request and returns the JSON response body.
    /// </summary>
    public async Task<string> GetJsonAsync(RequestInformation request, CancellationToken cancellationToken)
    {
        await using var body = await requestAdapter.SendPrimitiveAsync<Stream>(request, errorMapping, cancellationToken)
            ?? throw new CliException("unexpected-response", "The API returned no content.", ExitCodes.UnexpectedError);
        using var reader = new StreamReader(body);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a request whose response has no body (<c>204</c>).
    /// </summary>
    public Task SendAsync(RequestInformation request, CancellationToken cancellationToken) =>
        requestAdapter.SendNoContentAsync(request, errorMapping, cancellationToken);

    /// <summary>
    /// Writes a response document: the JSON with <c>--json</c>, otherwise as text.
    /// </summary>
    public async Task WriteAsync<T>(
        string json, ParsableFactory<T> factory, Action<TextWriter, T> writeText, CancellationToken cancellationToken)
        where T : IParsable
    {
        if (jsonOutput)
        {
            WriteJson(json);
            return;
        }

        var document = await KiotaJsonSerializer.DeserializeAsync(json, factory, cancellationToken)
            ?? throw new CliException("unexpected-response", "The API returned an empty document.", ExitCodes.UnexpectedError);
        writeText(context.Output, document);
    }

    /// <summary>
    /// Writes a response list: the JSON with <c>--json</c>, otherwise as text.
    /// </summary>
    public async Task WriteListAsync<T>(
        string json, ParsableFactory<T> factory, Action<TextWriter, IReadOnlyList<T>> writeText, CancellationToken cancellationToken)
        where T : IParsable
    {
        if (jsonOutput)
        {
            WriteJson(json);
            return;
        }

        var items = await KiotaJsonSerializer.DeserializeCollectionAsync(json, factory, cancellationToken);
        writeText(context.Output, [.. items]);
    }

    /// <summary>
    /// Writes a message for commands whose response has no body. Nothing with <c>--json</c>, like the API.
    /// </summary>
    public void WriteMessage(string message)
    {
        if (!jsonOutput)
        {
            context.Output.WriteLine(message);
        }
    }

    private void WriteJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        context.Output.WriteLine(JsonSerializer.Serialize(document.RootElement, indentedJson));
    }

    private static void EnsureJsonObject(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new CliException("invalid-input", "The input must be a JSON object (the complete document).");
            }
        }
        catch (JsonException exception)
        {
            throw new CliException("invalid-input", $"The input is not valid JSON: {exception.Message}");
        }
    }
}
