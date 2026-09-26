using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraderCli.ApiClient.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace GraderCli.Errors;

/// <summary>
/// An error as reported on stderr: as text, or as JSON with <c>--json</c>.
/// </summary>
/// <param name="Code">The API's error code (the <c>code</c> of the problem details) or an error code of the CLI.</param>
/// <param name="Message">Human-readable message; for API errors the problem details' <c>detail</c>.</param>
/// <param name="ExitCode">Exit code of the CLI (see <see cref="ExitCodes"/>).</param>
/// <param name="Status">HTTP status code for errors returned by the API.</param>
/// <param name="Errors">Field errors of <c>validation-failed</c>.</param>
internal sealed record CliError(
    string Code,
    string Message,
    [property: JsonIgnore] int ExitCode,
    int? Status = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors = null)
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static CliError FromException(Exception exception, string apiUrl) => exception switch
    {
        CliException cliException => new(cliException.Code, cliException.Message, cliException.ExitCode),
        HttpValidationProblemDetails problem => FromProblemDetails(problem),
        // Error response without problem details, e.g. from a server that is not the Grader API
        ApiException apiException => new(
            "unexpected-response",
            $"The API returned an unexpected response (HTTP {apiException.ResponseStatusCode}).",
            ExitCodes.ForHttpStatus(apiException.ResponseStatusCode),
            apiException.ResponseStatusCode),
        HttpRequestException or TaskCanceledException { InnerException: TimeoutException } => new(
            "api-unreachable",
            $"The Grader API at {apiUrl} is not reachable ({exception.Message}). "
                + $"Start the API or set --api-url or {Commands.GraderRootCommand.ApiUrlVariable}.",
            ExitCodes.ApiUnreachable),
        _ => new("unexpected-error", exception.Message, ExitCodes.UnexpectedError),
    };

    public void WriteTo(TextWriter error, bool asJson)
    {
        if (asJson)
        {
            error.WriteLine(JsonSerializer.Serialize(this, jsonOptions));
            return;
        }

        error.WriteLine(Status is null ? $"Error: {Message} [{Code}]" : $"Error: {Message} [{Code}, HTTP {Status}]");
        foreach (var (field, messages) in Errors ?? new Dictionary<string, IReadOnlyList<string>>())
        {
            foreach (var message in messages)
            {
                error.WriteLine($"  {field}: {message}");
            }
        }
    }

    private static CliError FromProblemDetails(HttpValidationProblemDetails problem)
    {
        var status = problem.Status ?? problem.ResponseStatusCode;
        var code = problem.AdditionalData.TryGetValue("code", out var value) && value is string text ? text : "unexpected-response";
        var message = problem.Detail ?? problem.Title ?? $"The API returned HTTP {status}.";
        var errors = problem.Errors?.AdditionalData.ToDictionary(
            error => error.Key,
            error => (IReadOnlyList<string>)[.. (error.Value as UntypedArray)?.GetValue().OfType<UntypedString>().Select(item => item.GetValue() ?? "") ?? []]);
        return new(code, message, ExitCodes.ForHttpStatus(status), status, errors is { Count: > 0 } ? errors : null);
    }
}
