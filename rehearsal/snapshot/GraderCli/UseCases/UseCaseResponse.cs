using GraderCli.Errors;
using Microsoft.Kiota.Abstractions.Serialization;

namespace GraderCli.UseCases;

/// <summary>
/// The response of a use case: a JSON document, a JSON list, or no body. The JSON is passed on unchanged (CLI with
/// <c>--json</c>, MCP tools); only the CLI's text output reads it into generated models.
/// </summary>
internal abstract class UseCaseResponse
{
    /// <summary>
    /// <see langword="false"/> for responses without body (<c>204</c>).
    /// </summary>
    public abstract bool HasBody { get; }

    public static UseCaseResponse Document<T>(ParsableFactory<T> factory, Action<TextWriter, T> writeText)
        where T : IParsable => new DocumentResponse<T>(factory, writeText);

    public static UseCaseResponse List<T>(ParsableFactory<T> factory, Action<TextWriter, IReadOnlyList<T>> writeText)
        where T : IParsable => new ListResponse<T>(factory, writeText);

    /// <param name="message">Confirmation of what was done, e.g. <c>Deleted class 7.</c>, for the ids of the request.</param>
    public static UseCaseResponse None(Func<IReadOnlyList<int>, string> message) => new NoBodyResponse(message);

    /// <summary>
    /// Writes the response as readable text. The <paramref name="json"/> body is <see langword="null"/> if
    /// <see cref="HasBody"/> is <see langword="false"/>.
    /// </summary>
    public abstract Task WriteTextAsync(TextWriter output, string? json, IReadOnlyList<int> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Confirmation for responses without body; <see langword="null"/> for responses with body.
    /// </summary>
    public virtual string? GetMessage(IReadOnlyList<int> ids) => null;

    private sealed class DocumentResponse<T>(ParsableFactory<T> factory, Action<TextWriter, T> writeText) : UseCaseResponse
        where T : IParsable
    {
        public override bool HasBody => true;

        public override async Task WriteTextAsync(
            TextWriter output, string? json, IReadOnlyList<int> ids, CancellationToken cancellationToken)
        {
            var document = await KiotaJsonSerializer.DeserializeAsync(json ?? "", factory, cancellationToken)
                ?? throw new CliException("unexpected-response", "The API returned an empty document.", ExitCodes.UnexpectedError);
            writeText(output, document);
        }
    }

    private sealed class ListResponse<T>(ParsableFactory<T> factory, Action<TextWriter, IReadOnlyList<T>> writeText) : UseCaseResponse
        where T : IParsable
    {
        public override bool HasBody => true;

        public override async Task WriteTextAsync(
            TextWriter output, string? json, IReadOnlyList<int> ids, CancellationToken cancellationToken)
        {
            var items = await KiotaJsonSerializer.DeserializeCollectionAsync(json ?? "", factory, cancellationToken);
            writeText(output, [.. items]);
        }
    }

    private sealed class NoBodyResponse(Func<IReadOnlyList<int>, string> message) : UseCaseResponse
    {
        public override bool HasBody => false;

        public override Task WriteTextAsync(TextWriter output, string? json, IReadOnlyList<int> ids, CancellationToken cancellationToken)
        {
            output.WriteLine(message(ids));
            return Task.CompletedTask;
        }

        public override string? GetMessage(IReadOnlyList<int> ids) => message(ids);
    }
}
