using System.Text;
using GraderCli.ApiClient;
using GraderCli.ApiClient.Models;
using GraderCli.Errors;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace GraderCli.UseCases;

/// <summary>
/// Calls the API for a use case, for CLI commands and MCP tools alike.
/// </summary>
/// <remarks>
/// Requests are built with the generated Kiota request builders, but request documents are sent and responses returned
/// as JSON unchanged instead of using Kiota's serializer: it never writes <c>null</c>, while the API requires explicit
/// nulls (e.g. the id of a new student) and returns them (e.g. a missing result). See tech-docs/cli.md.
/// </remarks>
internal static class ApiCaller
{
    // Every error response is read as (validation) problem details, a superset of problem details. Unlike the
    // generated methods' error mappings, this includes status codes an endpoint does not declare (e.g. 500).
    private static readonly Dictionary<string, ParsableFactory<IParsable>> errorMapping = new()
    {
        ["XXX"] = HttpValidationProblemDetails.CreateFromDiscriminatorValue,
    };

    /// <summary>
    /// Fails unless the API URL is an absolute http(s) URL.
    /// </summary>
    public static void ValidateApiUrl(string apiUrl)
    {
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri) || apiUri.Scheme is not ("http" or "https"))
        {
            throw new CliException("invalid-api-url", $"The API URL '{apiUrl}' is not an absolute http(s) URL.");
        }
    }

    /// <summary>
    /// Sends the request of a use case and returns the JSON response body (<see langword="null"/> for responses without
    /// body). The <paramref name="document"/> must be checked with <see cref="InputDocument.Validate"/> (<see langword="null"/>
    /// for use cases without document). Errors are thrown; <see cref="CliError.FromException"/> turns them into error codes.
    /// </summary>
    public static async Task<string?> CallAsync(
        UseCase useCase, IReadOnlyList<int> ids, string? document, string apiUrl, Func<HttpClient> createHttpClient,
        CancellationToken cancellationToken)
    {
        ValidateApiUrl(apiUrl);
        using var httpClient = createHttpClient();
        // Set BaseUrl before creating the client: its constructor copies it into the request builders.
        using var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
        {
            BaseUrl = apiUrl,
        };
        var request = useCase.CreateRequest(new GraderApiClient(requestAdapter), ids);
        if (document is not null)
        {
            request.SetStreamContent(new MemoryStream(Encoding.UTF8.GetBytes(document)), "application/json");
        }

        if (!useCase.Response.HasBody)
        {
            await requestAdapter.SendNoContentAsync(request, errorMapping, cancellationToken);
            return null;
        }

        await using var body = await requestAdapter.SendPrimitiveAsync<Stream>(request, errorMapping, cancellationToken)
            ?? throw new CliException("unexpected-response", "The API returned no content.", ExitCodes.UnexpectedError);
        using var reader = new StreamReader(body);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
