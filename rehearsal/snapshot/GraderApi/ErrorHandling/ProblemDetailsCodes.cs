namespace GraderApi.ErrorHandling;

/// <summary>
/// Ensures that problem details written by the framework also carry an error code in the <c>code</c>
/// extension member, like the ones for domain errors (see <see cref="GraderLogicExceptionHandler"/>).
/// </summary>
public static class ProblemDetailsCodes
{
    public const string ExtensionName = "code";

    public const string ValidationFailed = "validation-failed";

    public const string InvalidRequest = "invalid-request";

    public const string UnexpectedError = "unexpected-error";

    /// <summary>
    /// Adds a code for request validation errors, unreadable requests (e.g. invalid JSON or a missing property)
    /// and unexpected errors if the problem details have none yet.
    /// Used as <see cref="ProblemDetailsOptions.CustomizeProblemDetails"/>.
    /// </summary>
    public static void AddMissingCode(ProblemDetailsContext context)
    {
        var problemDetails = context.ProblemDetails;
        if (problemDetails.Extensions.ContainsKey(ExtensionName))
        {
            return;
        }

        var code = problemDetails switch
        {
            HttpValidationProblemDetails => ValidationFailed,
            { Status: StatusCodes.Status400BadRequest } => InvalidRequest,
            { Status: StatusCodes.Status500InternalServerError } => UnexpectedError,
            _ => null,
        };
        if (code is not null)
        {
            problemDetails.Extensions[ExtensionName] = code;
        }
    }
}
