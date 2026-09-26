using System.Net;
using GraderApi.Calculator;
using GraderLogic.Calculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GraderTests.Api;

/// <summary>
/// Unit tests for the calculator endpoints: HTTP pipeline is real, <see cref="ICalculator"/> is mocked.
/// </summary>
public class CalculatorEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ICalculator calculator = Substitute.For<ICalculator>();

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(calculator)))
        .CreateClient();

    [Fact]
    public async Task Add_ReturnsResultFromLogic()
    {
        calculator.Add(1, 2).Returns(42);

        var response = await CreateClient().GetAsync("/api/calculator/add?a=1&b=2", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AddResult>(TestContext.Current.CancellationToken);
        Assert.Equal(new AddResult(42), result);
        calculator.Received(1).Add(1, 2);
    }

    [Fact]
    public async Task Add_LogicException_ReturnsProblemDetails()
    {
        calculator.Add(Arg.Any<int>(), Arg.Any<int>()).Throws(new NegativeParameterException("a", -1));

        var response = await CreateClient().GetAsync("/api/calculator/add?a=-1&b=2", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(400, problem?.Status);
        Assert.Contains("'a'", problem?.Detail);
    }
}
