using System.Text.Json;
using GraderCli.Errors;

namespace GraderCli.UseCases;

/// <summary>
/// Reads and checks input documents of <c>create</c>/<c>save</c> use cases. The CLI and the MCP tools only check that
/// the input is a JSON object; validation and business rules are left to the API.
/// </summary>
internal static class InputDocument
{
    public static async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CliException("input-unreadable", $"Cannot read the file '{path}': {exception.Message}");
        }
    }

    /// <summary>
    /// Fails unless the document is a JSON object.
    /// </summary>
    public static void Validate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new CliException("input-missing", "The JSON document is empty.");
        }

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
