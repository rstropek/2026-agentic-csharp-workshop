using System.Runtime.CompilerServices;

namespace GraderLogic.Calculator;

public class NegativeParameterException(string parameterName, int value)
    : GraderLogicException($"Parameter '{parameterName}' must not be negative, but was {value}.")
{
    public string ParameterName { get; } = parameterName;

    public int Value { get; } = value;

    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        if (value < 0)
        {
            throw new NegativeParameterException(parameterName, value);
        }
    }
}
