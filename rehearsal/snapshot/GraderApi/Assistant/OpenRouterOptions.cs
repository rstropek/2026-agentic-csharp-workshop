namespace GraderApi.Assistant;

/// <summary>
/// The LLM of the assistant: OpenRouter's OpenAI-compatible Chat Completions API (configuration section
/// <c>OpenRouter</c>, see tech-docs/assistant.md).
/// </summary>
public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public Uri Endpoint { get; set; } = new("https://openrouter.ai/api/v1");

    /// <summary>OpenRouter's model id, e.g. <c>z-ai/glm-5.3-flash</c>.</summary>
    public string Model { get; set; } = "";

    /// <summary>A secret: user secrets or environment variable <c>OpenRouter__ApiKey</c>, never in the repository.</summary>
    public string? ApiKey { get; set; }
}
