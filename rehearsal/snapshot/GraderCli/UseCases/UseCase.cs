using GraderCli.ApiClient;
using Microsoft.Kiota.Abstractions;

namespace GraderCli.UseCases;

/// <summary>
/// An area of the API (a resource, e.g. <c>classes</c>) with its use cases: a command with subcommands in the CLI.
/// </summary>
internal sealed record UseCaseArea(string Name, string Description, params IReadOnlyList<UseCase> UseCases);

/// <summary>
/// One use case of the Web API. It is the single definition of both the CLI command <c>grader {area} {verb}</c> and the
/// MCP tool <c>{area}_{verb}</c>, which share description, parameters, request and error handling (see tech-docs/cli.md).
/// </summary>
internal sealed class UseCase
{
    public required string Verb { get; init; }

    /// <summary>
    /// What the use case does, incl. the API's error codes. Help text of the command and description of the tool, so
    /// it must not refer to CLI syntax.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Examples of the CLI command, shown in its help.
    /// </summary>
    public IReadOnlyList<string> Examples { get; init; } = [];

    /// <summary>
    /// The ids in the request URL, in the order <see cref="CreateRequest"/> expects them.
    /// </summary>
    public IReadOnlyList<IdParameter> Ids { get; init; } = [];

    /// <summary>
    /// Name of the schema of the request document in the API's OpenAPI spec (e.g. <c>SaveClassRequest</c>);
    /// <see langword="null"/> if the use case takes no document.
    /// </summary>
    public string? DocumentSchema { get; init; }

    public required UseCaseEffect Effect { get; init; }

    /// <summary>
    /// Builds the request with the generated Kiota request builders. The body of requests with a document is replaced
    /// by the document as read (see <see cref="ApiCaller"/>), so pass a placeholder model (<c>new()</c>).
    /// </summary>
    public required Func<GraderApiClient, IReadOnlyList<int>, RequestInformation> CreateRequest { get; init; }

    public required UseCaseResponse Response { get; init; }
}

/// <summary>
/// An id in the request URL: an argument of the command (<c>class-id</c>) and a parameter of the tool (<c>classId</c>).
/// The <paramref name="Name"/> is kebab-case.
/// </summary>
internal sealed record IdParameter(string Name, string Description);

/// <summary>
/// What a use case does to the data. Determines whether the CLI requires <c>--yes</c> and the annotations of the tool.
/// </summary>
internal enum UseCaseEffect
{
    /// <summary>Only reads data (<c>list</c>, <c>get</c>).</summary>
    Read,

    /// <summary>Adds data, but never changes or removes existing data (<c>create</c>).</summary>
    Create,

    /// <summary>
    /// Replaces a document incl. its lists, which removes entries missing in the input (<c>save</c>). Idempotent.
    /// </summary>
    Replace,

    /// <summary>Deletes data; cannot be undone (<c>delete</c>, <c>demo-data replace</c>). The CLI requires <c>--yes</c>.</summary>
    Delete,
}
