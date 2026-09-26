using GraderApi.Tools;
using ModelContextProtocol.Protocol;

namespace GraderApi.Mcp;

/// <summary>
/// Turns expected errors of a tool call into MCP tool errors (<c>isError: true</c>) whose text is the
/// <see cref="ToolError"/> as JSON, so the model can correct its call (see tech-docs/mcp.md).
/// </summary>
internal static class ToolErrors
{
    /// <summary>
    /// Returns the tool error for an expected error, or <see langword="null"/> for an unexpected one, which the SDK
    /// logs and reports as tool error without details.
    /// </summary>
    public static CallToolResult? TryCreate(Exception exception) => ToolError.TryCreate(exception) is { } error
        ? new()
        {
            IsError = true,
            Content = [new TextContentBlock { Text = error.ToJson() }],
        }
        : null;
}
