using System.Reflection;
using System.Text.Json;
using GraderApi.ClassManagement;
using GraderApi.DemoData;
using GraderApi.Exams;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace GraderApi.Tools;

/// <summary>
/// The use cases as tools for AI agents, independent of how they are offered: the MCP server at <c>/mcp</c> adapts them
/// to MCP, an in-app agent can pass them to a chat client (see tech-docs/mcp.md). The tools are the methods with
/// <see cref="ToolAttribute"/> of the tool classes, in the order of <see cref="ToolClasses"/> and of their declaration
/// (the order of <c>grader mcp</c>).
/// </summary>
/// <param name="jsonOptions">
/// The JSON options of the endpoints: tools read arguments and write results like the endpoints read requests and
/// write responses (e.g. required constructor parameters, strict numbers, explicit nulls).
/// </param>
public sealed class ToolCatalog(IOptions<JsonOptions> jsonOptions)
{
    /// <summary>
    /// The tool classes, registered as scoped services (see <see cref="ToolSetup.AddGraderTools"/>).
    /// </summary>
    internal static readonly IReadOnlyList<Type> ToolClasses = [typeof(ClassManagementTools), typeof(ExamTools), typeof(DemoDataTools)];

    private readonly JsonSerializerOptions serializerOptions = jsonOptions.Value.SerializerOptions;

    public IReadOnlyList<CatalogTool> Tools => field ??= [.. ToolClasses.SelectMany(GetToolMethods).Select(Create)];

    /// <summary>
    /// The tool methods of a tool class in declaration order (the order of the metadata tokens; reflection does not
    /// guarantee any order).
    /// </summary>
    private static IEnumerable<MethodInfo> GetToolMethods(Type toolClass) => toolClass
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(method => method.IsDefined(typeof(ToolAttribute)))
        .OrderBy(method => method.MetadataToken);

    /// <summary>
    /// Creates the tool of a method. The instance of the tool class comes from the services of the call
    /// (<see cref="AIFunctionArguments.Services"/>), so it gets the scoped services of the caller's scope, e.g. of the
    /// HTTP request.
    /// </summary>
    private CatalogTool Create(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<ToolAttribute>()!;
        var function = AIFunctionFactory.Create(
            method,
            arguments => (arguments.Services ?? throw new InvalidOperationException(
                $"Tool {attribute.Name} needs the services of the call (AIFunctionArguments.Services).")).GetRequiredService(method.DeclaringType!),
            new AIFunctionFactoryOptions
            {
                Name = attribute.Name,
                SerializerOptions = serializerOptions,
                MarshalResult = MarshalResult,
            });
        return new(function, attribute.Effect);
    }

    /// <summary>
    /// Results as JSON with the endpoints' options, but texts (e.g. the confirmation of a delete) as they are instead of
    /// as JSON strings, so the model reads them as text.
    /// </summary>
    private ValueTask<object?> MarshalResult(object? result, Type? resultType, CancellationToken cancellationToken) =>
        ValueTask.FromResult<object?>(result is string text
            ? text
            : JsonSerializer.SerializeToElement(result, serializerOptions.GetTypeInfo(resultType ?? typeof(object))));
}
