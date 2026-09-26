using System.Text.Json;
using GraderLogic;
using GraderLogic.ClassManagement;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GraderTests.Cli;

/// <summary>
/// Unit tests for the MCP server of the CLI: MCP client, real server and HTTP pipeline of the API,
/// <see cref="IClassManagementService"/> mocked.
/// </summary>
public class McpServerTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    private readonly IClassManagementService service = Substitute.For<IClassManagementService>();

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => service)))
        .CreateClient();

    [Fact]
    public async Task ClassesGetTool_ApiReturnsNotFound_ReturnsToolErrorWithErrorCode()
    {
        var ct = TestContext.Current.CancellationToken;
        service.GetClassAsync(7, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("class-not-found", "Class 7 was not found."));
        await using var mcp = await McpServerRunner.StartAsync(CreateClient);

        var result = await mcp.Client.CallToolAsync("classes_get", new Dictionary<string, object?> { ["classId"] = 7 }, cancellationToken: ct);

        // A tool error (not a protocol error), so the model can read and handle it.
        Assert.True(result.IsError);
        using var error = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.Equal("class-not-found", error.RootElement.GetProperty("code").GetString());
        Assert.Equal(404, error.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ListTools_OneToolPerCommand_AnnotatesReadOnlyAndDestructiveTools()
    {
        await using var mcp = await McpServerRunner.StartAsync(CreateClient);

        var tools = (await mcp.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ToDictionary(tool => tool.Name, tool => tool.ProtocolTool.Annotations);

        Assert.Equal(13, tools.Count);
        Assert.True(tools["classes_get"]?.ReadOnlyHint);
        Assert.False(tools["classes_create"]?.DestructiveHint);
        Assert.True(tools["classes_save"]?.DestructiveHint);
        Assert.True(tools["exams_delete"]?.DestructiveHint);
        Assert.True(tools["demo_data_replace"]?.DestructiveHint);
    }
}
