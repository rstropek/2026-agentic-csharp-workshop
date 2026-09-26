namespace GraderLogic;

/// <summary>
/// An id in the request (usually in the route) does not exist.
/// </summary>
public class NotFoundException(string code, string message) : GraderLogicException(code, message);
