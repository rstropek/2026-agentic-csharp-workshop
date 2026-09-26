using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraderApi.ErrorHandling;
using GraderApi.Validation;
using GraderLogic;

namespace GraderApi.Tools;

/// <summary>
/// An expected error of a tool call as the model reads it: the error as JSON, in the same shape as the tool errors of
/// <c>grader mcp</c> (<c>code</c>, <c>message</c>, <c>status</c>, <c>errors</c>), so the model can correct its call. Codes
/// and statuses are those of the API's problem details (see tech-docs/mcp.md). Hosts decide how to return it (MCP: a
/// tool error, assistant: the tool result).
/// </summary>
public sealed record ToolError(
    string Code,
    string Message,
    int Status,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors = null)
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <summary>
    /// Returns the tool error for an expected error, or <see langword="null"/> for an unexpected one, which hosts report
    /// without details.
    /// </summary>
    public static ToolError? TryCreate(Exception exception) => exception switch
    {
        GraderLogicException logicException => new(
            logicException.Code, logicException.Message, GraderLogicExceptionHandler.GetStatusCode(logicException)),
        RequestValidationException validationException => new(
            ProblemDetailsCodes.ValidationFailed, validationException.Message, StatusCodes.Status400BadRequest, validationException.Errors),
        // Arguments that do not fit the tool's parameters (e.g. a missing id, a missing property of a document)
        ArgumentException or JsonException => new(
            ProblemDetailsCodes.InvalidRequest, exception.Message, StatusCodes.Status400BadRequest),
        _ => null,
    };

    public string ToJson() => JsonSerializer.Serialize(this, jsonOptions);
}
