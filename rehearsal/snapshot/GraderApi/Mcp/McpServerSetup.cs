using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GraderApi.Mcp;

/// <summary>
/// The MCP server of the Web API (Streamable HTTP, stateless), with the tools of <c>grader mcp</c> (see tech-docs/mcp.md).
/// </summary>
public static class McpServerSetup
{
    /// <summary>
    /// The same instructions as <c>grader mcp</c>.
    /// </summary>
    private const string Instructions = "Grader manages the classes, students, exams and results of a teacher through the "
        + "Grader Web API. The tools mirror the API, one tool per use case. Never guess ids: resolve them by name, top-down "
        + "(school_years_list, classes_list, then classes_get for students and exams_list for exams; courses_list for courses). "
        + "Tools that save take the complete document and replace its lists: get it, change only what is needed, save it. "
        + "A percentage of null means no result, not 0 %. Errors are tool errors with the API's error code.";

    /// <summary>
    /// Adds the MCP server. The tool classes must be registered as services.
    /// </summary>
    public static IServiceCollection AddGraderMcpServer(this IServiceCollection services)
    {
        services.AddMcpServer().WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless);
        services.AddSingleton<IConfigureOptions<McpServerOptions>, ConfigureMcpServerOptions>();
        return services;
    }

    /// <summary>
    /// Configures the server with list/call handlers instead of the SDK's tool collection (a dictionary), so tools are
    /// always listed in the catalog's order, like <c>grader mcp</c> does.
    /// </summary>
    /// <param name="jsonOptions">
    /// The JSON options of the endpoints: tools read arguments and write results like the endpoints read requests and
    /// write responses (e.g. required constructor parameters, explicit nulls).
    /// </param>
    private sealed class ConfigureMcpServerOptions(IOptions<JsonOptions> jsonOptions) : IConfigureOptions<McpServerOptions>
    {
        public void Configure(McpServerOptions options)
        {
            var tools = ToolCatalog.Create(jsonOptions.Value.SerializerOptions);
            options.ServerInfo = new()
            {
                Name = "grader",
                Title = "Grader",
                Version = typeof(McpServerSetup).Assembly.GetName().Version?.ToString(3) ?? "",
            };
            options.ServerInstructions = Instructions;
            options.Handlers.ListToolsHandler = (_, _) =>
                ValueTask.FromResult(new ListToolsResult { Tools = [.. tools.Select(tool => tool.ProtocolTool)] });
            options.Handlers.CallToolHandler = async (request, cancellationToken) =>
            {
                var tool = tools.FirstOrDefault(tool => tool.ProtocolTool.Name == request.Params!.Name)
                    ?? throw new McpProtocolException($"Unknown tool: '{request.Params!.Name}'", McpErrorCode.InvalidParams);
                try
                {
                    return await tool.InvokeAsync(request, cancellationToken);
                }
                catch (Exception exception) when (ToolErrors.TryCreate(exception) is { } error)
                {
                    return error;
                }
            };
        }
    }
}
