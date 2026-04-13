using System.Text.Json;

namespace KanelBrief.Core.Serialization;

/// <summary>
/// Single source of truth for the JSON serialization contract shared between the
/// backend HTTP API, the repository layer, and the Next.js frontend.
/// </summary>
/// <remarks>
/// The frontend types in <c>frontend/lib/types.ts</c> declare camelCase property
/// names, so every payload that crosses the wire must be serialized with
/// <see cref="JsonNamingPolicy.CamelCase"/>. Azure Functions'
/// <c>HttpResponseData.WriteAsJsonAsync</c> defaults to PascalCase, so endpoints
/// must serialize explicitly with <see cref="CamelCase"/> instead.
/// </remarks>
public static class KanelJsonOptions
{
    /// <summary>
    /// camelCase JSON options used for all API responses and repository-level
    /// serialization of nested objects. Immutable and safe to reuse.
    /// </summary>
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
