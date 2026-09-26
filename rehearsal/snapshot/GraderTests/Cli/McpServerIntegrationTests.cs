using System.Text.Json;
using GraderLogic.Data;
using ModelContextProtocol.Protocol;

namespace GraderTests.Cli;

/// <summary>
/// Integration tests for the MCP server of the CLI: MCP client, real server, real API, real logic, in-memory SQLite database.
/// </summary>
[Trait("Category", "Integration")]
public class McpServerIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task ClassesGetTool_CalledThroughMcpClient_ReturnsClassFromApi()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = factory.CreateDbContext();
        var schoolClass = new SchoolClass
        {
            Name = "4D",
            SchoolYear = new SchoolYear { Name = "2026/27" },
            Students = [new Student { FirstName = "Alan", LastName = "Turing" }, new Student { FirstName = "Ada", LastName = "Lovelace" }],
        };
        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(ct);
        await using var mcp = await McpServerRunner.StartAsync(factory.CreateClient);

        var result = await mcp.Client.CallToolAsync("classes_get", new Dictionary<string, object?> { ["classId"] = schoolClass.Id }, cancellationToken: ct);

        Assert.Equal(McpServerRunner.ProtocolVersion, mcp.Client.NegotiatedProtocolVersion);
        Assert.NotEqual(true, result.IsError);
        using var json = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.Equal("4D", json.RootElement.GetProperty("name").GetString());
        Assert.Equal(
            ["Lovelace", "Turing"],
            json.RootElement.GetProperty("students").EnumerateArray().Select(student => student.GetProperty("lastName").GetString()));
    }
}
