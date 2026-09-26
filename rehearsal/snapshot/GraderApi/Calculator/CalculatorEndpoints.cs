using System.ComponentModel;
using GraderLogic.Calculator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GraderApi.Calculator;

public static class CalculatorEndpoints
{
    public static IEndpointRouteBuilder MapCalculatorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/calculator").WithTags("Calculator");

        group.MapGet("/add", Add)
            .WithName("Add")
            .WithSummary("Adds two numbers")
            .WithDescription("Adds two non-negative integers. Negative parameters are rejected with a problem details response.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    private static Ok<AddResult> Add(
        [Description("First summand, must not be negative.")] int a,
        [Description("Second summand, must not be negative.")] int b,
        ICalculator calculator)
        => TypedResults.Ok(new AddResult(calculator.Add(a, b)));
}

[Description("Result of an addition.")]
public record AddResult([property: Description("Sum of the two parameters.")] int Result);
