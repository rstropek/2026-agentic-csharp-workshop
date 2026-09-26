namespace GraderApi.Tools;

/// <summary>
/// Makes a method of a tool class a tool of the <see cref="ToolCatalog"/>. Together with the <c>[Description]</c> of the
/// method and its parameters, this is the complete definition of the tool (see tech-docs/mcp.md).
/// </summary>
/// <param name="name">Name of the tool, <c>&lt;area&gt;_&lt;verb&gt;</c> like the command of the CLI (e.g. <c>classes_get</c>).</param>
/// <param name="effect">What the tool does to the data; determines its traits.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolAttribute(string name, ToolEffect effect) : Attribute
{
    public string Name { get; } = name;

    public ToolEffect Effect { get; } = effect;
}
