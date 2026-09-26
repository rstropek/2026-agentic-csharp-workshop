using System.Text.Json;
using GraderApi.Tools;
using GraderLogic.ClassManagement;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GraderTests.Api;

/// <summary>
/// Unit tests for the <see cref="ToolCatalog"/>: tools invoked as <see cref="AIFunction"/>, like an in-app agent does,
/// without MCP; the API's services, <see cref="IClassManagementService"/> mocked.
/// </summary>
public class ToolCatalogTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    private readonly IClassManagementService service = Substitute.For<IClassManagementService>();

    private IServiceProvider CreateServices() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => service)))
        .Services;

    [Fact]
    public async Task ClassesGetTool_InvokedAsAIFunction_ReturnsClassAsJson()
    {
        var ct = TestContext.Current.CancellationToken;
        service.GetClassAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ClassDetails(7, "1A", 3, "2025/26", [new ClassStudent(12, "Ada", "Lovelace")]));
        var services = CreateServices();
        var tool = services.GetRequiredService<ToolCatalog>().Tools.Single(tool => tool.Name == "classes_get");

        // The tool class is resolved from the services of the call, e.g. the scope of the agent's request.
        await using var scope = services.CreateAsyncScope();
        var result = await tool.InvokeAsync(new AIFunctionArguments { ["classId"] = 7, Services = scope.ServiceProvider }, ct);

        Assert.True(tool.IsReadOnly);
        Assert.False(tool.IsDestructive);
        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("1A", json.GetProperty("name").GetString());
        Assert.Equal("Lovelace", json.GetProperty("students")[0].GetProperty("lastName").GetString());
    }
}
