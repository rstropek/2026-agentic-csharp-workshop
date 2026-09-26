using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraderTests.Integration;

/// <summary>
/// Smoke tests for the API documentation endpoints (only mapped in Development,
/// which is the default environment of <see cref="WebApplicationFactory{TEntryPoint}"/>).
/// </summary>
[Trait("Category", "Integration")]
public class ApiDocsTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Get_Succeeds(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownApiRoute_ReturnsNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
