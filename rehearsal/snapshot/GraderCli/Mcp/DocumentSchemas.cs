using System.Text.Json.Nodes;

namespace GraderCli.Mcp;

/// <summary>
/// JSON schemas of the input documents, from the API's OpenAPI spec (embedded resource, the same spec the API client is
/// generated from). OpenAPI 3.1+ schemas are JSON Schema 2020-12, the default dialect of MCP.
/// </summary>
internal static class DocumentSchemas
{
    private const string SchemaRefPrefix = "#/components/schemas/";

    private static readonly Lazy<JsonObject> schemas = new(LoadSchemas);

    /// <summary>
    /// Returns a copy of the schema with all references to other schemas inlined, so the tool's input schema is
    /// self-contained.
    /// </summary>
    public static JsonObject Get(string name) => Resolve(name, []);

    private static JsonObject Resolve(string name, HashSet<string> resolving)
    {
        var schema = schemas.Value[name] as JsonObject
            ?? throw new InvalidOperationException($"The OpenAPI spec has no schema '{name}'.");
        if (!resolving.Add(name))
        {
            throw new InvalidOperationException($"The schema '{name}' is recursive and cannot be inlined.");
        }

        var resolved = (JsonObject)InlineReferences(schema, resolving)!;
        resolving.Remove(name);
        return resolved;
    }

    private static JsonNode? InlineReferences(JsonNode? node, HashSet<string> resolving)
    {
        switch (node)
        {
            case JsonObject obj when obj["$ref"]?.GetValue<string>() is { } reference && reference.StartsWith(SchemaRefPrefix, StringComparison.Ordinal):
                // Keywords next to $ref (e.g. a description) override the referenced schema's.
                var resolved = Resolve(reference[SchemaRefPrefix.Length..], resolving);
                foreach (var (key, value) in obj.Where(property => property.Key != "$ref"))
                {
                    resolved[key] = InlineReferences(value, resolving);
                }

                return resolved;
            case JsonObject obj:
                return new JsonObject(obj.Select(property =>
                    KeyValuePair.Create(property.Key, InlineReferences(property.Value, resolving))));
            case JsonArray array:
                return new JsonArray([.. array.Select(item => InlineReferences(item, resolving))]);
            default:
                return node?.DeepClone();
        }
    }

    private static JsonObject LoadSchemas()
    {
        using var stream = typeof(DocumentSchemas).Assembly.GetManifestResourceStream("GraderApi.json")
            ?? throw new InvalidOperationException("The OpenAPI spec is not embedded.");
        return JsonNode.Parse(stream)?["components"]?["schemas"] as JsonObject
            ?? throw new InvalidOperationException("The OpenAPI spec has no schemas.");
    }
}
