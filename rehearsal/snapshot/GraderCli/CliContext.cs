namespace GraderCli;

/// <summary>
/// The process environment of a CLI run: standard streams, environment variables and HTTP.
/// <see cref="FromConsole"/> is the real one; tests provide their own to run commands in memory.
/// </summary>
public sealed class CliContext
{
    public required TextWriter Output { get; init; }

    public required TextWriter Error { get; init; }

    public required TextReader Input { get; init; }

    /// <summary>
    /// <see langword="false"/> if stdin is a terminal. The CLI then never reads it: it must never wait for input.
    /// </summary>
    public required bool IsInputRedirected { get; init; }

    public required Func<string, string?> GetEnvironmentVariable { get; init; }

    /// <summary>
    /// Creates the HTTP client for the API calls of one command; the CLI disposes it.
    /// </summary>
    public required Func<HttpClient> CreateHttpClient { get; init; }

    public static CliContext FromConsole() => new()
    {
        Output = Console.Out,
        Error = Console.Error,
        Input = Console.In,
        IsInputRedirected = Console.IsInputRedirected,
        GetEnvironmentVariable = Environment.GetEnvironmentVariable,
        CreateHttpClient = () => new HttpClient(),
    };
}
