using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using GraderApi.ClassManagement;
using GraderApi.DemoData;
using GraderApi.Exams;
using ModelContextProtocol.Server;

namespace GraderApi.Mcp;

/// <summary>
/// The MCP tools of the Web API: the methods of the tool classes with the names, annotations and order of the tools of
/// <c>grader mcp</c> (see tech-docs/mcp.md). Names and annotations are MCP-specific, so they live here instead of on
/// the methods.
/// </summary>
internal static class ToolCatalog
{
    public static IReadOnlyList<McpServerTool> Create(JsonSerializerOptions serializerOptions) =>
    [
        Create<ClassManagementTools>("school_years_list", nameof(ClassManagementTools.GetSchoolYearsAsync), ToolEffect.Read, serializerOptions),
        Create<ClassManagementTools>("classes_list", nameof(ClassManagementTools.GetClassesAsync), ToolEffect.Read, serializerOptions),
        Create<ClassManagementTools>("classes_get", nameof(ClassManagementTools.GetClassAsync), ToolEffect.Read, serializerOptions),
        Create<ClassManagementTools>("classes_create", nameof(ClassManagementTools.CreateClassAsync), ToolEffect.Create, serializerOptions),
        Create<ClassManagementTools>("classes_save", nameof(ClassManagementTools.SaveClassAsync), ToolEffect.Replace, serializerOptions),
        Create<ClassManagementTools>("classes_delete", nameof(ClassManagementTools.DeleteClassAsync), ToolEffect.Delete, serializerOptions),
        Create<ExamTools>("courses_list", nameof(ExamTools.GetCoursesAsync), ToolEffect.Read, serializerOptions),
        Create<ExamTools>("exams_list", nameof(ExamTools.GetExamsAsync), ToolEffect.Read, serializerOptions),
        Create<ExamTools>("exams_get", nameof(ExamTools.GetExamAsync), ToolEffect.Read, serializerOptions),
        Create<ExamTools>("exams_create", nameof(ExamTools.CreateExamAsync), ToolEffect.Create, serializerOptions),
        Create<ExamTools>("exams_save", nameof(ExamTools.SaveExamAsync), ToolEffect.Replace, serializerOptions),
        Create<ExamTools>("exams_delete", nameof(ExamTools.DeleteExamAsync), ToolEffect.Delete, serializerOptions),
        Create<DemoDataTools>("demo_data_replace", nameof(DemoDataTools.ReplaceWithDemoDataAsync), ToolEffect.Delete, serializerOptions),
    ];

    /// <summary>
    /// Creates the tool of a method. The instance of the tool class comes from the request's services (in stateless
    /// mode those of the HTTP request), so it gets the scoped services of the request.
    /// </summary>
    private static McpServerTool Create<TTools>(string name, string methodName, ToolEffect effect, JsonSerializerOptions serializerOptions)
        where TTools : notnull
    {
        var method = typeof(TTools).GetMethod(methodName)
            ?? throw new InvalidOperationException($"{typeof(TTools).Name} has no method {methodName}.");
        var isReadOnly = effect == ToolEffect.Read;
        var tool = McpServerTool.Create(method, request => request.Services!.GetRequiredService<TTools>(), new McpServerToolCreateOptions
        {
            Name = name,
            SerializerOptions = serializerOptions,
            ReadOnly = isReadOnly,
            Destructive = isReadOnly ? null : effect is ToolEffect.Replace or ToolEffect.Delete,
            Idempotent = isReadOnly ? null : effect == ToolEffect.Replace,
            OpenWorld = false,
        });
        // Only the method's description, like grader mcp: the SDK appends the description of the result type.
        tool.ProtocolTool.Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return tool;
    }

    /// <summary>
    /// What a tool does to the data; determines its annotations (like the use case effect of the CLI).
    /// </summary>
    private enum ToolEffect
    {
        /// <summary>Only reads data.</summary>
        Read,

        /// <summary>Adds data, but never changes or removes existing data.</summary>
        Create,

        /// <summary>Replaces a document incl. its lists, which removes entries missing in the input. Idempotent.</summary>
        Replace,

        /// <summary>Deletes data; cannot be undone.</summary>
        Delete,
    }
}
