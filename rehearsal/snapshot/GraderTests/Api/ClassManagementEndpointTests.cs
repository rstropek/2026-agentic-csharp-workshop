using System.Net;
using GraderLogic;
using GraderLogic.ClassManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GraderTests.Api;

/// <summary>
/// Unit tests for the class management endpoints: HTTP pipeline is real, <see cref="IClassManagementService"/> is mocked.
/// </summary>
public class ClassManagementEndpointTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    private static readonly SaveClassRequest validRequest = new("1A", [new SaveClassStudent(1, "Ada", "Lovelace"), new SaveClassStudent(null, "Alan", "Turing")]);

    private readonly IClassManagementService service = Substitute.For<IClassManagementService>();

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => service)))
        .CreateClient();

    [Fact]
    public async Task SaveClass_ReturnsSavedClassFromService()
    {
        var ct = TestContext.Current.CancellationToken;
        var saved = new ClassDetails(7, "1A", 3, "2025/26", [new ClassStudent(1, "Ada", "Lovelace"), new ClassStudent(2, "Alan", "Turing")]);
        service.SaveClassAsync(7, Arg.Any<SaveClassRequest>(), Arg.Any<CancellationToken>()).Returns(saved);

        var response = await CreateClient().PutAsJsonAsync("/api/classes/7", validRequest, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ClassDetails>(ct);
        Assert.Equal(saved.Id, result?.Id);
        Assert.Equal(saved.Students, result?.Students);
        await service.Received(1).SaveClassAsync(
            7,
            Arg.Is<SaveClassRequest>(request => request.Name == "1A" && request.Students.SequenceEqual(validRequest.Students)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveClass_ServiceConflict_ReturnsConflictProblemDetailsWithCode()
    {
        var ct = TestContext.Current.CancellationToken;
        service.SaveClassAsync(Arg.Any<int>(), Arg.Any<SaveClassRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("student-has-results", "Students with exam results cannot be removed."));

        var response = await CreateClient().PutAsJsonAsync("/api/classes/7", validRequest, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal(409, problem?.Status);
        Assert.Equal("student-has-results", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task SaveClass_InvalidRequest_ReturnsValidationProblemDetailsWithoutCallingService()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = validRequest with { Name = "", Students = [new SaveClassStudent(null, "Ada", new string('x', 76))] };

        var response = await CreateClient().PutAsJsonAsync("/api/classes/7", request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        Assert.Equal("validation-failed", problem?.Extensions["code"]?.ToString());
        Assert.Contains("Name", problem!.Errors.Keys);
        Assert.Contains("Students[0].LastName", problem.Errors.Keys);
        await service.DidNotReceiveWithAnyArgs().SaveClassAsync(default, default!, ct);
    }
}
