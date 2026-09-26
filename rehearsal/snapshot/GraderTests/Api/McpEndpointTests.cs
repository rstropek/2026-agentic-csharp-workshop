using System.Text.Json;
using GraderLogic;
using GraderLogic.ClassManagement;
using GraderTests.Cli;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GraderTests.Api;

/// <summary>
/// Unit tests for the MCP server of the API (<c>/mcp</c>): MCP client, real server and HTTP pipeline,
/// <see cref="IClassManagementService"/> mocked.
/// </summary>
public class McpEndpointTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    private readonly IClassManagementService service = Substitute.For<IClassManagementService>();

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => service)))
        .CreateClient();

    [Fact]
    public async Task ClassesSaveTool_ServiceViolatesBusinessRule_ReturnsToolErrorWithErrorCode()
    {
        var ct = TestContext.Current.CancellationToken;
        service.SaveClassAsync(7, Arg.Any<SaveClassRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BusinessRuleException("duplicate-student", "Student 12 appears more than once."));
        await using var client = await ApiMcpClient.ConnectAsync(CreateClient());

        var result = await client.CallToolAsync("classes_save", new Dictionary<string, object?>
        {
            ["classId"] = 7,
            ["document"] = JsonElement.Parse("""
                {"name": "1A", "students": [{"id": 12, "firstName": "Ada", "lastName": "Lovelace"}, {"id": 12, "firstName": "Ada", "lastName": "Lovelace"}]}
                """),
        }, cancellationToken: ct);

        // A tool error (not a protocol error), so the model can read and handle it.
        Assert.True(result.IsError);
        using var error = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.Equal("duplicate-student", error.RootElement.GetProperty("code").GetString());
        Assert.Equal(400, error.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ListTools_SameToolsAsCliMcpServer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var apiClient = await ApiMcpClient.ConnectAsync(CreateClient());
        await using var cli = await McpServerRunner.StartAsync(CreateClient);

        var apiTools = await apiClient.ListToolsAsync(cancellationToken: ct);
        var cliTools = await cli.Client.ListToolsAsync(cancellationToken: ct);

        // Same names (and order), descriptions and annotations; the parameters differ (no local files).
        Assert.Equal(cliTools.Select(Describe), apiTools.Select(Describe));
        Assert.Equal(cli.Client.ServerInstructions, apiClient.ServerInstructions);
    }

    private static string Describe(McpClientTool tool) => JsonSerializer.Serialize(new
    {
        tool.Name,
        tool.Description,
        tool.ProtocolTool.Annotations,
    });
}
