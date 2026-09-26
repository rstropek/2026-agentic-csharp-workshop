using GraderCli;

namespace GraderTests.Cli;

/// <summary>
/// Runs the CLI in memory (like the <c>grader</c> command) and captures stdout, stderr and the exit code.
/// </summary>
public static class CliRunner
{
    /// <summary>
    /// Runs the CLI with the command-line arguments <paramref name="args"/>.
    /// </summary>
    /// <param name="createHttpClient">Creates the client for the API, e.g. <c>factory.CreateClient</c> of a <see cref="GraderApiFactory"/>.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="input">Content of stdin; <see langword="null"/> for a terminal (not redirected).</param>
    public static async Task<CliResult> RunAsync(Func<HttpClient> createHttpClient, string[] args, string? input = null)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var reader = new StringReader(input ?? "");
        var context = new CliContext
        {
            Output = output,
            Error = error,
            Input = reader,
            IsInputRedirected = input is not null,
            GetEnvironmentVariable = _ => null,
            CreateHttpClient = createHttpClient,
        };

        // The in-memory test server accepts any host; the URL makes the tests independent of the default.
        var exitCode = await CliApp.RunAsync([.. args, "--api-url", "http://localhost"], context, TestContext.Current.CancellationToken);
        return new(exitCode, output.ToString(), error.ToString());
    }
}

public sealed record CliResult(int ExitCode, string Output, string Error);
