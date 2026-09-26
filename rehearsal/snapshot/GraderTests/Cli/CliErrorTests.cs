using System.Text.Json;
using GraderCli.Errors;
using GraderLogic;
using GraderLogic.ClassManagement;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GraderTests.Cli;

/// <summary>
/// Unit tests for the error handling of the CLI: real CLI and HTTP pipeline of the API, <see cref="IClassManagementService"/> mocked.
/// </summary>
public class CliErrorTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    private readonly IClassManagementService service = Substitute.For<IClassManagementService>();

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => service)))
        .CreateClient();

    [Fact]
    public async Task ClassesGet_ApiReturnsNotFound_WritesMessageWithCodeToStderrAndReturnsNotFound()
    {
        service.GetClassAsync(7, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("class-not-found", "Class 7 was not found."));

        var result = await CliRunner.RunAsync(CreateClient, ["classes", "get", "7"]);

        Assert.Equal(ExitCodes.NotFound, result.ExitCode);
        Assert.Empty(result.Output);
        Assert.Equal("Error: Class 7 was not found. [class-not-found, HTTP 404]", result.Error.TrimEnd());
    }

    [Fact]
    public async Task ClassesGet_ApiReturnsNotFoundWithJson_WritesErrorAsJsonToStderr()
    {
        service.GetClassAsync(7, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("class-not-found", "Class 7 was not found."));

        var result = await CliRunner.RunAsync(CreateClient, ["classes", "get", "7", "--json"]);

        Assert.Equal(ExitCodes.NotFound, result.ExitCode);
        Assert.Empty(result.Output);
        using var error = JsonDocument.Parse(result.Error);
        Assert.Equal("class-not-found", error.RootElement.GetProperty("code").GetString());
        Assert.Equal(404, error.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ClassesSave_ApiReturnsValidationProblem_WritesFieldErrorsAndReturnsInvalidRequest()
    {
        var result = await CliRunner.RunAsync(CreateClient, ["classes", "save", "7"], input: """{ "name": "", "students": [] }""");

        Assert.Equal(ExitCodes.InvalidRequest, result.ExitCode);
        Assert.Contains("[validation-failed, HTTP 400]", result.Error);
        Assert.Contains("  Name: ", result.Error);
        await service.DidNotReceiveWithAnyArgs().SaveClassAsync(default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClassesDelete_WithoutYes_ReturnsUsageErrorWithoutCallingApi()
    {
        var result = await CliRunner.RunAsync(CreateClient, ["classes", "delete", "7"]);

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("[confirmation-required]", result.Error);
        await service.DidNotReceiveWithAnyArgs().DeleteClassAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClassesSave_NoFileAndStdinIsTerminal_ReturnsUsageErrorInsteadOfWaitingForInput()
    {
        var result = await CliRunner.RunAsync(CreateClient, ["classes", "save", "7"], input: null);

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("[input-missing]", result.Error);
    }

    [Fact]
    public async Task AnyCommand_ApiNotReachable_ReturnsApiUnreachable()
    {
        var result = await CliRunner.RunAsync(() => new HttpClient(new UnreachableHandler()), ["school-years", "list"]);

        Assert.Equal(ExitCodes.ApiUnreachable, result.ExitCode);
        Assert.Contains("[api-unreachable]", result.Error);
    }

    [Fact]
    public async Task Help_ForCommand_ShowsExamplesAndSucceeds()
    {
        var result = await CliRunner.RunAsync(CreateClient, ["classes", "get", "--help"]);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Examples:", result.Output);
        Assert.Contains("grader classes get 7", result.Output);
    }

    private sealed class UnreachableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused (localhost:5100)");
    }
}
