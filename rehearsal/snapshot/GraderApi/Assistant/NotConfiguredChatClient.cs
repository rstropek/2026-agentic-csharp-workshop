using Microsoft.Extensions.AI;

namespace GraderApi.Assistant;

/// <summary>
/// Stands in for the LLM if no API key is configured, so the app starts without one and only the assistant fails: its
/// endpoint answers 503 with the code <c>assistant-not-configured</c> (see <see cref="AssistantSetup"/>).
/// </summary>
internal sealed class NotConfiguredChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw CreateException();

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw CreateException();

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static InvalidOperationException CreateException() => new(
        $"The assistant needs an API key for OpenRouter (configuration value {OpenRouterOptions.SectionName}:ApiKey).");
}
