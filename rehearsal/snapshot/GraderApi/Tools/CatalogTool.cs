using Microsoft.Extensions.AI;

namespace GraderApi.Tools;

/// <summary>
/// A tool of the <see cref="ToolCatalog"/>: an <see cref="AIFunction"/> (name, description, parameter schema, invocation)
/// with the traits that hosts need to decide how to offer it, e.g. to ask the user before destructive calls.
/// </summary>
public sealed class CatalogTool(AIFunction function, ToolEffect effect) : DelegatingAIFunction(function)
{
    public ToolEffect Effect { get; } = effect;

    /// <summary>Only reads data.</summary>
    public bool IsReadOnly => Effect == ToolEffect.Read;

    /// <summary>Changes or removes existing data (a save replaces lists and removes entries missing in the input).</summary>
    public bool IsDestructive => Effect is ToolEffect.Replace or ToolEffect.Delete;

    /// <summary>Calling it again with the same arguments has no additional effect.</summary>
    public bool IsIdempotent => Effect is ToolEffect.Read or ToolEffect.Replace;
}
