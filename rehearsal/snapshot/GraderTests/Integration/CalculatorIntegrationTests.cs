using System.Net;
using GraderApi.Calculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraderTests.Integration;

/// <summary>
/// Integration tests for the calculator endpoints with the real logic.
/// </summary>
[Trait("Category", "Integration")]
public class CalculatorIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Add_ReturnsSum()
    {
        var response = await factory.CreateClient().GetAsync("/api/calculator/add?a=1&b=2", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AddResult>(TestContext.Current.CancellationToken);
        Assert.Equal(new AddResult(3), result);
    }

    [Fact]
    public async Task Add_NegativeParameter_ReturnsProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/calculator/add?a=1&b=-2", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(400, problem?.Status);
        Assert.Contains("'b'", problem?.Detail);
    }
}
