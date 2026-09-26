namespace GraderLogic;

/// <summary>
/// Base class for all expected domain errors raised by the grading logic (see tech-docs/web-api.md).
/// </summary>
/// <remarks>
/// The API translates these exceptions into problem details with <see cref="Code"/> in the <c>code</c>
/// extension member: <see cref="NotFoundException"/> becomes <c>404</c>, <see cref="ConflictException"/>
/// <c>409</c>, all others <c>400</c>. Unexpected errors must not derive from this class so that they
/// surface as <c>500</c> responses.
/// </remarks>
/// <param name="code">Stable, kebab-case error code (e.g. <c>class-name-taken</c>) that clients can rely on.</param>
/// <param name="message">Human-readable description of the error.</param>
/// <param name="innerException">Optional cause of the error.</param>
public abstract class GraderLogicException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
