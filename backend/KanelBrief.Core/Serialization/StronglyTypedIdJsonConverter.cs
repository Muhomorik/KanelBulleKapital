using System.Text.Json;
using System.Text.Json.Serialization;

namespace KanelBrief.Core.Serialization;

/// <summary>
/// Serializes strongly-typed ID structs as plain JSON strings rather than objects.
/// Register one instance per ID type in <see cref="KanelJsonOptions"/>.
/// </summary>
public sealed class StronglyTypedIdJsonConverter<T>(Func<string, T> fromString, Func<T, string> toString)
    : JsonConverter<T> where T : struct
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => fromString(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(toString(value));
}
