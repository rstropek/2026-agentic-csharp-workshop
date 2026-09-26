using System.Net;
using System.Text.Json;
using GraderApi.Assistant;

namespace GraderTests.Api;

/// <summary>
/// Unit tests for the assistant's endpoint (<c>/api/assistant</c>) without an LLM. For a complete AG-UI run, see
/// <c>AssistantIntegrationTests</c>.
/// </summary>
public class AssistantEndpointTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task Run_NoApiKeyConfigured_Returns503WithErrorCode()
    {
        var ct = TestContext.Current.CancellationToken;

        // The tests configure no API key, so the app started without one.
        using var response = await factory.CreateClient().PostAsJsonAsync("/api/assistant", new
        {
            threadId = "thread-1",
            runId = "run-1",
            messages = new[] { new { id = "message-1", role = "user", content = "Hello" } },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { },
        }, ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(AssistantSetup.NotConfiguredCode, problem.GetProperty("code").GetString());
    }
}
