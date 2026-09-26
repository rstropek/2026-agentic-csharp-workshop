namespace GraderLogic;

/// <summary>
/// Base class for all expected domain errors raised by the grading logic.
/// </summary>
/// <remarks>
/// The API translates every exception derived from this class into a
/// <c>400 Bad Request</c> problem details response. Unexpected errors must
/// not derive from this class so that they surface as <c>500</c> responses.
/// </remarks>
public abstract class GraderLogicException(string message) : Exception(message);
