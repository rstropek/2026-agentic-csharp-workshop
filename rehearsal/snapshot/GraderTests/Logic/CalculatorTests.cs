using GraderLogic.Calculator;

namespace GraderTests.Logic;

public class CalculatorTests
{
    private readonly Calculator calculator = new();

    [Fact]
    public void Add_ReturnsSum()
    {
        Assert.Equal(3, calculator.Add(1, 2));
    }

    [Theory]
    [InlineData(-1, 2, "a")]
    [InlineData(1, -2, "b")]
    public void Add_NegativeParameter_Throws(int a, int b, string expectedParameterName)
    {
        var ex = Assert.Throws<NegativeParameterException>(() => calculator.Add(a, b));
        Assert.Equal(expectedParameterName, ex.ParameterName);
    }
}
