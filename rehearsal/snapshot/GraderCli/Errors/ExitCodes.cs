namespace GraderCli.Errors;

/// <summary>
/// Exit codes of the CLI, documented in <c>grader --help</c> and tech-docs/cli.md. Scripts and agents rely on them:
/// never change the meaning of a code.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;

    /// <summary>
    /// Unexpected error, incl. server errors (5xx) and unexpected responses of the API.
    /// </summary>
    public const int UnexpectedError = 1;

    /// <summary>
    /// Invalid command line or input document, or a destructive command without <c>--yes</c>. Nothing was sent to the API.
    /// </summary>
    public const int UsageError = 2;

    /// <summary>
    /// The API rejected the request (400), e.g. <c>validation-failed</c> or a violated business rule.
    /// </summary>
    public const int InvalidRequest = 3;

    /// <summary>
    /// An id does not exist (404).
    /// </summary>
    public const int NotFound = 4;

    /// <summary>
    /// Conflict with existing data (409), e.g. a duplicate name or data that is still referenced.
    /// </summary>
    public const int Conflict = 5;

    /// <summary>
    /// The API is not reachable (connection failed or timed out).
    /// </summary>
    public const int ApiUnreachable = 6;

    public static int ForHttpStatus(int status) => status switch
    {
        400 => InvalidRequest,
        404 => NotFound,
        409 => Conflict,
        _ => UnexpectedError,
    };
}
