using System.Text.Json;
using System.Text.Json.Nodes;
using GraderCli.Errors;
using GraderCli.UseCases;
using ModelContextProtocol.Protocol;

namespace GraderCli.Mcp;

/// <summary>
/// The MCP tool of a use case: <c>{area}_{verb}</c> with the ids as integer parameters and, for use cases with a
/// document, the document inline (<c>document</c>) or as path of a local JSON file (<c>file</c>).
/// </summary>
internal sealed class UseCaseTool
{
    private const string DocumentParameter = "document";
    private const string FileParameter = "file";

    private readonly UseCase useCase;
    private readonly IReadOnlyList<string> idParameters;
    private readonly IReadOnlyList<string> parameters;

    public UseCaseTool(UseCaseArea area, UseCase useCase)
    {
        this.useCase = useCase;
        idParameters = [.. useCase.Ids.Select(id => ToCamelCase(id.Name))];
        parameters = useCase.DocumentSchema is null ? idParameters : [.. idParameters, DocumentParameter, FileParameter];
        ProtocolTool = new Tool
        {
            Name = $"{area.Name}_{useCase.Verb}".Replace('-', '_'),
            Description = useCase.Description,
            InputSchema = CreateInputSchema(),
            Annotations = useCase.Effect == UseCaseEffect.Read
                ? new ToolAnnotations { ReadOnlyHint = true, OpenWorldHint = false }
                : new ToolAnnotations
                {
                    ReadOnlyHint = false,
                    DestructiveHint = useCase.Effect is UseCaseEffect.Replace or UseCaseEffect.Delete,
                    IdempotentHint = useCase.Effect == UseCaseEffect.Replace,
                    OpenWorldHint = false,
                },
        };
    }

    public Tool ProtocolTool { get; }

    /// <summary>
    /// Calls the API. The result is the API's JSON response (or a confirmation for responses without body); every
    /// error becomes a tool error with the error as JSON (see <see cref="CliError"/>), so the model can correct it.
    /// </summary>
    public async ValueTask<CallToolResult> CallAsync(
        IDictionary<string, JsonElement>? arguments, string apiUrl, Func<HttpClient> createHttpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            arguments ??= new Dictionary<string, JsonElement>();
            var unknown = arguments.Keys.Except(parameters).ToList();
            if (unknown.Count > 0)
            {
                throw new CliException("invalid-input", $"Unknown parameters: {string.Join(", ", unknown)}.");
            }

            var ids = idParameters.Select(name => GetId(arguments, name)).ToList();
            var document = useCase.DocumentSchema is null ? null : await GetDocumentAsync(arguments, cancellationToken);
            var json = await ApiCaller.CallAsync(useCase, ids, document, apiUrl, createHttpClient, cancellationToken);
            return new CallToolResult { Content = [new TextContentBlock { Text = json ?? useCase.Response.GetMessage(ids) ?? "" }] };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var error = CliError.FromException(exception, apiUrl);
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = error.ToJson() }] };
        }
    }

    private JsonElement CreateInputSchema()
    {
        var properties = new JsonObject();
        foreach (var (id, name) in useCase.Ids.Zip(idParameters))
        {
            properties[name] = new JsonObject { ["type"] = "integer", ["description"] = id.Description };
        }

        if (useCase.DocumentSchema is not null)
        {
            var document = DocumentSchemas.Get(useCase.DocumentSchema);
            document["description"] = $"{document["description"]} Pass either {DocumentParameter} or {FileParameter}.";
            properties[DocumentParameter] = document;
            properties[FileParameter] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"Path of a local JSON file with the document ({useCase.DocumentSchema}), instead of "
                    + $"{DocumentParameter}. Absolute, or relative to the working directory of the MCP server.",
            };
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray([.. idParameters.Select(name => JsonValue.Create(name))]),
            ["additionalProperties"] = false,
        };
        return JsonElement.Parse(schema.ToJsonString());
    }

    private static int GetId(IDictionary<string, JsonElement> arguments, string name) =>
        !arguments.TryGetValue(name, out var value) ? throw new CliException("invalid-input", $"The parameter {name} is missing.")
        : value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var id) ? id
        : throw new CliException("invalid-input", $"The parameter {name} must be an integer id.");

    private static async Task<string> GetDocumentAsync(IDictionary<string, JsonElement> arguments, CancellationToken cancellationToken)
    {
        var hasDocument = arguments.TryGetValue(DocumentParameter, out var document);
        var hasFile = arguments.TryGetValue(FileParameter, out var file);
        var json = (hasDocument, hasFile) switch
        {
            // Raw text, not re-serialized: explicit nulls (e.g. the id of a new student) must reach the API.
            (true, false) => document.GetRawText(),
            (false, true) when file.ValueKind == JsonValueKind.String => await InputDocument.ReadFileAsync(file.GetString()!, cancellationToken),
            (false, true) => throw new CliException("invalid-input", $"The parameter {FileParameter} must be a path."),
            _ => throw new CliException("invalid-input", $"Pass the document either inline ({DocumentParameter}) or as path of a JSON file ({FileParameter})."),
        };
        InputDocument.Validate(json);
        return json;
    }

    private static string ToCamelCase(string kebabCase) =>
        string.Concat(kebabCase.Split('-').Select((part, index) => index == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
}
