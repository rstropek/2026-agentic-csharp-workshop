using System.Text.Json;
using GraderApi.Assistant;
using GraderLogic.Data;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTests.Integration;

/// <summary>
/// Integration tests for the assistant (<c>/api/assistant</c>): an AG-UI run over HTTP, real agent, tools, logic and
/// in-memory SQLite database; only the LLM is a fake (<see cref="ScriptedChatClient"/>).
/// </summary>
[Trait("Category", "Integration")]
public class AssistantIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task Run_ModelCallsTool_StreamsToolCallResultAndAnswer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var arrangeContext = factory.CreateDbContext())
        {
            arrangeContext.SchoolYears.Add(new SchoolYear { Name = "2031/32" });
            await arrangeContext.SaveChangesAsync(ct);
        }

        const string Answer = "The newest school year is **2031/32**.";
        var chatClient = new ScriptedChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "school_years_list")])),
            new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)));
        var httpClient = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddKeyedSingleton<IChatClient>(AssistantSetup.ChatClientKey, chatClient)))
            .CreateClient();

        // The run input of AG-UI (https://docs.ag-ui.com/sdk/js/core/types.md#runagentinput)
        using var response = await httpClient.PostAsJsonAsync("/api/assistant", new
        {
            threadId = "thread-1",
            runId = "run-1",
            messages = new[] { new { id = "message-1", role = "user", content = "Which is the newest school year?" } },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { },
        }, ct);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var events = ParseServerSentEvents(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal("RUN_STARTED", events[0].GetProperty("type").GetString());
        Assert.Equal("RUN_FINISHED", events[^1].GetProperty("type").GetString());

        var toolCallStart = Assert.Single(events, e => e.GetProperty("type").GetString() == "TOOL_CALL_START");
        Assert.Equal(("call-1", "school_years_list"),
            (toolCallStart.GetProperty("toolCallId").GetString(), toolCallStart.GetProperty("toolCallName").GetString()));
        var toolCallResult = Assert.Single(events, e => e.GetProperty("type").GetString() == "TOOL_CALL_RESULT");
        Assert.Equal("call-1", toolCallResult.GetProperty("toolCallId").GetString());
        using var result = JsonDocument.Parse(toolCallResult.GetProperty("content").GetString()!);
        Assert.Contains("2031/32", result.RootElement.EnumerateArray().Select(schoolYear => schoolYear.GetProperty("name").GetString()));
        Assert.Equal(Answer, string.Concat(events
            .Where(e => e.GetProperty("type").GetString() == "TEXT_MESSAGE_CONTENT")
            .Select(e => e.GetProperty("delta").GetString())));

        // The model only gets the read-only tools, and the result of the call before it answers.
        Assert.Equal(
            ["school_years_list", "classes_list", "classes_get", "courses_list", "exams_list", "exams_get"],
            chatClient.Requests[0].Options?.Tools?.Select(tool => tool.Name));
        var functionResult = Assert.Single(chatClient.Requests[1].Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>());
        Assert.Equal("call-1", functionResult.CallId);
    }

    /// <summary>
    /// The data of the events of a server-sent event stream, each a JSON object (AG-UI events have no event names).
    /// </summary>
    private static List<JsonElement> ParseServerSentEvents(string stream) => [.. stream
        .Split('\n')
        .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
        .Select(line => JsonElement.Parse(line["data:".Length..]))];
}
