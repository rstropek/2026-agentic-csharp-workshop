namespace GraderLogic.Calculator;

public class Calculator : ICalculator
{
    public int Add(int a, int b)
    {
        NegativeParameterException.ThrowIfNegative(a);
        NegativeParameterException.ThrowIfNegative(b);

        return checked(a + b);
    }
}
