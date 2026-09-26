using GraderLogic;
using Microsoft.AspNetCore.Diagnostics;

namespace GraderApi.ErrorHandling;

/// <summary>
/// Translates expected domain errors (<see cref="GraderLogicException"/>) into problem details with the
/// error code in the <c>code</c> extension member (see tech-docs/web-api.md). All other exceptions are
/// left to the default handler (<c>500</c>).
/// </summary>
public sealed class GraderLogicExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not GraderLogicException logicException)
        {
            return false;
        }

        var status = GetStatusCode(logicException);
        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Detail = exception.Message,
                Extensions = { [ProblemDetailsCodes.ExtensionName] = logicException.Code },
            },
        });
    }

    /// <summary>
    /// The HTTP status code of a domain error, also reported by the MCP tools (see tech-docs/mcp.md).
    /// </summary>
    public static int GetStatusCode(GraderLogicException exception) => exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
