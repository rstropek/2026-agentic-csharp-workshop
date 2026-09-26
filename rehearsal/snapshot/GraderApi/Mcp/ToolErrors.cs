using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraderApi.ErrorHandling;
using GraderApi.Validation;
using GraderLogic;
using ModelContextProtocol.Protocol;

namespace GraderApi.Mcp;

/// <summary>
/// Turns expected errors of a tool call into tool errors (<c>isError: true</c>) whose text is the error as JSON, in
/// the same shape as the tool errors of <c>grader mcp</c> (<c>code</c>, <c>message</c>, <c>status</c>, <c>errors</c>),
/// so the model can correct its call. Codes and statuses are those of the API's problem details (see tech-docs/mcp.md).
/// </summary>
internal static class ToolErrors
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <summary>
    /// Returns the tool error for an expected error, or <see langword="null"/> for an unexpected one, which the SDK
    /// logs and reports as tool error without details.
    /// </summary>
    public static CallToolResult? TryCreate(Exception exception) => exception switch
    {
        GraderLogicException logicException => Create(new(
            logicException.Code, logicException.Message, GraderLogicExceptionHandler.GetStatusCode(logicException))),
        RequestValidationException validationException => Create(new(
            ProblemDetailsCodes.ValidationFailed, validationException.Message, StatusCodes.Status400BadRequest, validationException.Errors)),
        // Arguments that do not fit the tool's parameters (e.g. a missing id, a missing property of a document)
        ArgumentException or JsonException => Create(new(
            ProblemDetailsCodes.InvalidRequest, exception.Message, StatusCodes.Status400BadRequest)),
        _ => null,
    };

    private static CallToolResult Create(ToolError error) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = JsonSerializer.Serialize(error, jsonOptions) }],
    };

    private sealed record ToolError(
        string Code,
        string Message,
        int Status,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors = null);
}
