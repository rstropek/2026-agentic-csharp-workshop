using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace GraderApi.Validation;

/// <summary>
/// Validates a request record with the same rules and the same metadata as the minimal API validation of the endpoints
/// (see tech-docs/web-api.md), for callers that bypass the endpoints, e.g. the tools (see tech-docs/mcp.md).
/// </summary>
public sealed class RequestValidator(IOptions<ValidationOptions> options, IServiceProvider services)
{
    /// <exception cref="ArgumentNullException">The request is <see langword="null"/>.</exception>
    /// <exception cref="RequestValidationException">The request is invalid.</exception>
    public async Task ValidateAsync<TRequest>(
        TRequest? request, CancellationToken cancellationToken, [CallerArgumentExpression(nameof(request))] string? requestName = null)
        where TRequest : class
    {
        // Endpoints reject a missing body before validation; callers passing JSON null (e.g. a tool argument) end up here.
        ArgumentNullException.ThrowIfNull(request, requestName);

        // The metadata is generated for types used by endpoint handlers only. Without it, nothing would be validated.
        if (!options.Value.TryGetValidatableTypeInfo(typeof(TRequest), out var typeInfo))
        {
            throw new InvalidOperationException($"{typeof(TRequest).Name} has no validation metadata.");
        }

        var context = new ValidateContext { ValidationOptions = options.Value, ServiceProvider = services };
        await typeInfo.ValidateAsync(request, context, cancellationToken);
        if (context.ValidationErrors is { Count: > 0 } errors)
        {
            throw new RequestValidationException(errors.ToDictionary(
                error => error.Key,
                error => (IReadOnlyList<string>)[.. error.Value.Select(validationError => validationError.ErrorMessage)]));
        }
    }
}
