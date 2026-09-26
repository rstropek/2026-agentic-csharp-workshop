namespace GraderApi.Validation;

/// <summary>
/// A request record is invalid (see <see cref="RequestValidator"/>). The counterpart of the endpoints' validation
/// problem details (<c>validation-failed</c>).
/// </summary>
/// <param name="errors">Error messages per member path (e.g. <c>Students[0].LastName</c>), as in the problem details.</param>
public sealed class RequestValidationException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; } = errors;
}
