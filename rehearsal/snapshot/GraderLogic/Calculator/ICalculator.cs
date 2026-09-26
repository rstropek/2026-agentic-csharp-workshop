namespace GraderLogic.Calculator;

public interface ICalculator
{
    /// <summary>
    /// Adds two non-negative numbers.
    /// </summary>
    /// <exception cref="NegativeParameterException">Thrown if one of the parameters is negative.</exception>
    int Add(int a, int b);
}
