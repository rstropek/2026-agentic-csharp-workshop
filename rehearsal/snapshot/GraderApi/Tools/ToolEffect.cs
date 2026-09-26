namespace GraderApi.Tools;

/// <summary>
/// What a tool does to the data (like the use case effect of the CLI); determines the traits of a
/// <see cref="CatalogTool"/>.
/// </summary>
public enum ToolEffect
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
