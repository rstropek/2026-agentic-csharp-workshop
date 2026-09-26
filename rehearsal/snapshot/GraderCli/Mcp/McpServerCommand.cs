using System.CommandLine;
using GraderCli.Commands;
using GraderCli.UseCases;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GraderCli.Mcp;

/// <summary>
/// <c>grader mcp</c>: runs Grader as MCP server over stdio with one tool per use case (see tech-docs/cli.md).
/// </summary>
internal static class McpServerCommand
{
    private const string Instructions = "Grader manages the classes, students, exams and results of a teacher through the "
        + "Grader Web API. The tools mirror the API, one tool per use case. Never guess ids: resolve them by name, top-down "
        + "(school_years_list, classes_list, then classes_get for students and exams_list for exams; courses_list for courses). "
        + "Tools that save take the complete document and replace its lists: get it, change only what is needed, save it. "
        + "A percentage of null means no result, not 0 %. Errors are tool errors with the API's error code.";

    public static Command Create()
    {
        var command = new GraderCommand("mcp", "Runs Grader as MCP server (Model Context Protocol) over stdio for AI agents, "
            + "with one tool per command (e.g. classes_get). Runs until stdin is closed.",
            "grader mcp",
            "grader mcp --api-url http://localhost:5200");
        command.SetApiAction((call, ct) => RunAsync(call.Context, call.ApiUrl, ct));
        return command;
    }

    private static async Task RunAsync(CliContext context, string apiUrl, CancellationToken cancellationToken)
    {
        ApiCaller.ValidateApiUrl(apiUrl);
        var tools = UseCaseCatalog.Areas
            .SelectMany(area => area.UseCases.Select(useCase => new UseCaseTool(area, useCase)))
            .ToList();
        var options = new McpServerOptions
        {
            ServerInfo = new()
            {
                Name = "grader",
                Title = "Grader",
                Version = typeof(McpServerCommand).Assembly.GetName().Version?.ToString(3) ?? "",
            },
            ServerInstructions = Instructions,
            Handlers = new()
            {
                // Always in the same order (the catalog's), for clients caching the list.
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools.Select(tool => tool.ProtocolTool)] }),
                CallToolHandler = (request, ct) =>
                    tools.Find(tool => tool.ProtocolTool.Name == request.Params!.Name) is { } tool
                        ? tool.CallAsync(request.Params!.Arguments, apiUrl, context.CreateHttpClient, ct)
                        : throw new McpProtocolException($"Unknown tool: '{request.Params!.Name}'", McpErrorCode.InvalidParams),
            },
        };

        await using var transport = context.CreateMcpServerTransport();
        await using var server = McpServer.Create(transport, options);
        await server.RunAsync(cancellationToken);
    }
}
