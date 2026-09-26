namespace GraderLogic;

/// <summary>
/// The request conflicts with existing data (duplicate, still referenced, constraint violation).
/// </summary>
public class ConflictException(string code, string message, Exception? innerException = null)
    : GraderLogicException(code, message, innerException);
