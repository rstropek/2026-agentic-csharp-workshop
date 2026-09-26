using System.ClientModel;
using GraderApi.ErrorHandling;
using GraderApi.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace GraderApi.Assistant;

/// <summary>
/// The AI assistant: an agent (Microsoft Agent Framework) that answers a teacher's questions about classes, exams and
/// results with the read-only tools of the <see cref="ToolCatalog"/>, served over AG-UI (see tech-docs/assistant.md).
/// </summary>
public static class AssistantSetup
{
    /// <summary>The route of the AG-UI endpoint within <c>/api</c>.</summary>
    public const string Route = "/assistant";

    /// <summary>
    /// Key of the LLM's <see cref="IChatClient"/> (keyed singleton), e.g. for tests to replace it with a fake.
    /// </summary>
    public const string ChatClientKey = "assistant";

    public const string NotConfiguredCode = "assistant-not-configured";

    /// <summary>
    /// The system instructions. The markdown subset is what the web UI renders (see spec/design/assistant.md).
    /// </summary>
    internal const string Instructions = """
        You are the assistant of Grader, a grading app for teachers. You answer the teacher's questions about their
        school years, classes, students, courses, exams and results, using the tools.

        - Keep answers short and to the point. Answer in the language of the question.
        - Format answers only with paragraphs, **bold**, *italic*, `inline code`, links, bullet lists, numbered lists
          and tables. No headings, images, code blocks or nested lists.
        - Use the newest school year unless the teacher names another one (school_years_list lists them newest first).
        - Never invent data. State only what the tools returned; if they do not provide something, say so.
        - Never guess ids: resolve them by name, top-down (school_years_list, classes_list, then classes_get for
          students and exams_list for exams; courses_list for courses).
        - A percentage of null means no result, not 0 %.
        - You can only read data. If the teacher wants to change something, say that you cannot and that they can do
          it on the Classes and Exams pages.
        """;

    /// <summary>
    /// Adds the assistant. The tool catalog must be registered as service (<see cref="ToolSetup.AddGraderTools"/>).
    /// </summary>
    public static IServiceCollection AddGraderAssistant(this IServiceCollection services)
    {
        services.AddOptions<OpenRouterOptions>().BindConfiguration(OpenRouterOptions.SectionName);
        services.AddKeyedSingleton(ChatClientKey, (serviceProvider, _) =>
            CreateChatClient(serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value));
        services.AddSingleton(CreateAgent);
        services.AddAGUIServer();
        return services;
    }

    /// <summary>
    /// Maps the AG-UI endpoint (<c>POST</c>, the run input as JSON, the events as server-sent events).
    /// </summary>
    public static IEndpointConventionBuilder MapGraderAssistant(this IEndpointRouteBuilder endpoints) => endpoints
        .MapAGUIServer(Route, endpoints.ServiceProvider.GetRequiredService<AIAgent>())
        .AddEndpointFilter(RequireConfiguredChatClientAsync)
        // No REST endpoint for the generated API clients: the web UI talks to it with an AG-UI client.
        .ExcludeFromDescription();

    private static IChatClient CreateChatClient(OpenRouterOptions options) => string.IsNullOrWhiteSpace(options.ApiKey)
        ? new NotConfiguredChatClient()
        : new OpenAIClient(new ApiKeyCredential(options.ApiKey), new OpenAIClientOptions { Endpoint = options.Endpoint })
            .GetChatClient(options.Model)
            .AsIChatClient();

    /// <summary>
    /// Creates the agent, a singleton (the AG-UI endpoint resolves it once), with its own function invocation, so that
    /// tool calls get services of their own scope (see <see cref="InvokeToolAsync"/>).
    /// </summary>
    private static AIAgent CreateAgent(IServiceProvider services)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var chatClient = services.GetRequiredKeyedService<IChatClient>(ChatClientKey)
            .AsBuilder()
            .UseFunctionInvocation(loggerFactory, client =>
                client.FunctionInvoker = (context, cancellationToken) => InvokeToolAsync(scopeFactory, context, cancellationToken))
            .Build(services);
        IList<AITool> tools = [.. services.GetRequiredService<ToolCatalog>().Tools.Where(tool => tool.IsReadOnly)];
        return chatClient.AsAIAgent(
            instructions: Instructions,
            name: "GraderAssistant",
            description: "Answers a teacher's questions about classes, exams and results.",
            tools: tools,
            loggerFactory: loggerFactory,
            services: services);
    }

    /// <summary>
    /// Calls a tool with the services of a scope of its own: the tool classes and the <c>DbContext</c> are scoped, but
    /// the agent is a singleton. Expected errors become the tool's result, so the model can read and handle them;
    /// unexpected ones are left to the function invocation, which only tells the model that the call failed.
    /// </summary>
    private static async ValueTask<object?> InvokeToolAsync(
        IServiceScopeFactory scopeFactory, FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        context.Arguments.Services = scope.ServiceProvider;
        try
        {
            return await context.Function.InvokeAsync(context.Arguments, cancellationToken);
        }
        catch (Exception exception) when (ToolError.TryCreate(exception) is { } error)
        {
            return error.ToJson();
        }
    }

    /// <summary>
    /// Without an API key, the AG-UI stream would only report a generic run error (it hides exception details), so the
    /// endpoint answers with a problem instead.
    /// </summary>
    private static async ValueTask<object?> RequireConfiguredChatClientAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        => context.HttpContext.RequestServices.GetRequiredKeyedService<IChatClient>(ChatClientKey) is NotConfiguredChatClient
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The assistant is not configured.",
                detail: $"The API needs an API key for OpenRouter (configuration value {OpenRouterOptions.SectionName}:ApiKey).",
                extensions: new Dictionary<string, object?> { [ProblemDetailsCodes.ExtensionName] = NotConfiguredCode })
            : await next(context);
}
