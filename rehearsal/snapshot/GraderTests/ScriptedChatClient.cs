using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace GraderTests;

/// <summary>
/// Stands in for the assistant's LLM: answers each request with the next response of a script (e.g. a tool call, then
/// the answer) and records the requests, so tests can check what the model got (tools, tool results).
/// </summary>
public sealed class ScriptedChatClient(params ChatResponse[] responses) : IChatClient
{
    private readonly Queue<ChatResponse> script = new(responses);

    public List<(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // A copy: the caller keeps adding to its list of messages.
        Requests.Add(([.. messages], options));
        return Task.FromResult(script.TryDequeue(out var response)
            ? response
            : throw new InvalidOperationException($"The script has no response for request {Requests.Count}."));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
}
