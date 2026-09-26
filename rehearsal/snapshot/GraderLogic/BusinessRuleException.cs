namespace GraderLogic;

/// <summary>
/// The request content violates a business rule (e.g. a result for a student of another class).
/// </summary>
public class BusinessRuleException(string code, string message) : GraderLogicException(code, message);
