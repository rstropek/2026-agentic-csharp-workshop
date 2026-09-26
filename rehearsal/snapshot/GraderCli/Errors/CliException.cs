namespace GraderCli.Errors;

/// <summary>
/// An error detected by the CLI itself, e.g. a missing <c>--yes</c> or an invalid input document.
/// </summary>
/// <param name="code">Stable, kebab-case error code, like the API's error codes.</param>
/// <param name="message">Human-readable description of the error.</param>
/// <param name="exitCode">Exit code of the CLI (see <see cref="ExitCodes"/>).</param>
internal sealed class CliException(string code, string message, int exitCode = ExitCodes.UsageError) : Exception(message)
{
    public string Code { get; } = code;

    public int ExitCode { get; } = exitCode;
}
