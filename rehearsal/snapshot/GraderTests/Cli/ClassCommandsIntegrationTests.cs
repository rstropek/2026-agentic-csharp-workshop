using System.Text.Json;
using GraderCli.Errors;
using GraderLogic.Data;
using Microsoft.EntityFrameworkCore;

namespace GraderTests.Cli;

/// <summary>
/// Integration tests for the class commands of the CLI: real CLI, real API, real logic, in-memory SQLite database.
/// </summary>
[Trait("Category", "Integration")]
public class ClassCommandsIntegrationTests(GraderApiFactory factory) : IClassFixture<GraderApiFactory>
{
    [Fact]
    public async Task ClassesGet_WithJson_PrintsClassFromApi()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = factory.CreateDbContext();
        var schoolClass = new SchoolClass
        {
            Name = "1A",
            SchoolYear = new SchoolYear { Name = "2025/26" },
            Students = [new Student { FirstName = "Alan", LastName = "Turing" }, new Student { FirstName = "Ada", LastName = "Lovelace" }],
        };
        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(ct);

        var result = await CliRunner.RunAsync(factory.CreateClient, ["classes", "get", $"{schoolClass.Id}", "--json"]);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal("1A", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("2025/26", json.RootElement.GetProperty("schoolYearName").GetString());
        Assert.Equal(
            ["Lovelace", "Turing"],
            json.RootElement.GetProperty("students").EnumerateArray().Select(student => student.GetProperty("lastName").GetString()));
    }

    [Fact]
    public async Task ClassesSave_OutputOfClassesGet_SavesClassUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = factory.CreateDbContext();
        var schoolClass = new SchoolClass
        {
            Name = "3C",
            SchoolYear = new SchoolYear { Name = "2023/24" },
            Students = [new Student { FirstName = "Ada", LastName = "Lovelace" }],
        };
        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(ct);
        var get = await CliRunner.RunAsync(factory.CreateClient, ["classes", "get", $"{schoolClass.Id}", "--json"]);

        var save = await CliRunner.RunAsync(factory.CreateClient, ["classes", "save", $"{schoolClass.Id}", "--json"], input: get.Output);

        Assert.Equal(ExitCodes.Success, save.ExitCode);
        Assert.Empty(save.Error);
        Assert.Equal(get.Output, save.Output);
    }

    [Fact]
    public async Task ClassesCreate_DocumentFromStdinWithNullIds_CreatesClassAndPrintsIt()
    {
        // Kiota's serializer would drop "id": null, which the API requires; the CLI must send the document unchanged.
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = factory.CreateDbContext();
        var schoolYear = new SchoolYear { Name = "2024/25" };
        dbContext.SchoolYears.Add(schoolYear);
        await dbContext.SaveChangesAsync(ct);
        const string Document = """{ "name": "2B", "students": [{ "id": null, "firstName": "Grace", "lastName": "Hopper" }] }""";

        var result = await CliRunner.RunAsync(factory.CreateClient, ["classes", "create", $"{schoolYear.Id}"], input: Document);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Contains("Class 2B", result.Output);
        Assert.Contains("Hopper", result.Output);
        await using var verifyContext = factory.CreateDbContext();
        var created = await verifyContext.SchoolClasses.Include(schoolClass => schoolClass.Students)
            .SingleAsync(schoolClass => schoolClass.SchoolYearId == schoolYear.Id, ct);
        Assert.Equal("2B", created.Name);
        Assert.Equal("Hopper", Assert.Single(created.Students).LastName);
    }
}
