using GraderLogic;
using Microsoft.AspNetCore.Diagnostics;

namespace GraderApi.ErrorHandling;

/// <summary>
/// Translates expected domain errors (<see cref="GraderLogicException"/>) into
/// <c>400 Bad Request</c> problem details. All other exceptions are left to the
/// default handler (<c>500</c>).
/// </summary>
public sealed class GraderLogicExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not GraderLogicException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = exception.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            },
        });
    }
}
