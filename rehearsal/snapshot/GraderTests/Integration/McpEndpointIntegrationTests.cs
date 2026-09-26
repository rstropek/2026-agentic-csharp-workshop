using System.Text.Json;
using GraderLogic.Data;
using GraderTests.Cli;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Protocol;

namespace GraderTests.Integration;

/// <summary>
/// Integration tests for the MCP server of the API (<c>/mcp</c>): MCP client, real server, real logic, in-memory SQLite
/// database.
/// </summary>
[Trait("Category", "Integration")]
public class McpEndpointIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task ClassesCreateTool_CalledOverHttp_CreatesClassInDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        var schoolYear = new SchoolYear { Name = "2027/28" };
        await using (var arrangeContext = factory.CreateDbContext())
        {
            arrangeContext.SchoolYears.Add(schoolYear);
            await arrangeContext.SaveChangesAsync(ct);
        }

        await using var client = await ApiMcpClient.ConnectAsync(factory.CreateClient());

        // The document as JSON, incl. the explicit null id of a new student (see tech-docs/web-api.md).
        var result = await client.CallToolAsync("classes_create", new Dictionary<string, object?>
        {
            ["schoolYearId"] = schoolYear.Id,
            ["document"] = JsonElement.Parse("""{"name": "5E", "students": [{"id": null, "firstName": "Grace", "lastName": "Hopper"}]}"""),
        }, cancellationToken: ct);

        Assert.Equal(McpServerRunner.ProtocolVersion, client.NegotiatedProtocolVersion);
        Assert.NotEqual(true, result.IsError);
        using var json = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        var classId = json.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("Hopper", json.RootElement.GetProperty("students")[0].GetProperty("lastName").GetString());
        await using var verifyContext = factory.CreateDbContext();
        var saved = await verifyContext.SchoolClasses.Include(schoolClass => schoolClass.Students).SingleAsync(schoolClass => schoolClass.Id == classId, ct);
        Assert.Equal(("5E", schoolYear.Id), (saved.Name, saved.SchoolYearId));
        Assert.Equal("Grace", Assert.Single(saved.Students).FirstName);
    }
}
