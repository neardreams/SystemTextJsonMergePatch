using System.Text.Json;

namespace SystemTextJsonMergePatch;

/// <summary>
/// Compares two objects and produces a JsonDocument containing only the differences (RFC 7396 style).
/// </summary>
public static class DiffBuilder
{
    /// <summary>
    /// Build a JSON Merge Patch document representing the diff from original to patched.
    /// Returns a non-null JsonDocument (empty object {} if no differences).
    /// </summary>
    public static JsonDocument Build<T>(T original, T patched) where T : class
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var originalElement = JsonSerializer.SerializeToElement(original, options);
        var patchedElement = JsonSerializer.SerializeToElement(patched, options);

        var diffElement = BuildElement(originalElement, patchedElement);
        return JsonDocument.Parse(diffElement.GetRawText());
    }

    /// <summary>
    /// Internal: build a diff JsonElement from two JsonElements.
    /// </summary>
    internal static JsonElement BuildElement(JsonElement original, JsonElement patched)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteDiff(writer, original, patched);
        }
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static void WriteDiff(Utf8JsonWriter writer, JsonElement original, JsonElement patched)
    {
        // Both objects: recursive property diff
        if (original.ValueKind == JsonValueKind.Object && patched.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();

            // Properties in patched
            foreach (var prop in patched.EnumerateObject())
            {
                if (original.TryGetProperty(prop.Name, out var origValue))
                {
                    if (!JsonElementEquals(origValue, prop.Value))
                    {
                        writer.WritePropertyName(prop.Name);

                        if (origValue.ValueKind == JsonValueKind.Object && prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            WriteDiff(writer, origValue, prop.Value);
                        }
                        else
                        {
                            prop.Value.WriteTo(writer);
                        }
                    }
                }
                else
                {
                    // New property
                    writer.WritePropertyName(prop.Name);
                    prop.Value.WriteTo(writer);
                }
            }

            // Properties removed (in original but not in patched) → null
            foreach (var prop in original.EnumerateObject())
            {
                if (!patched.TryGetProperty(prop.Name, out _))
                {
                    writer.WritePropertyName(prop.Name);
                    writer.WriteNullValue();
                }
            }

            writer.WriteEndObject();
            return;
        }

        // Different or non-object: write patched value directly
        patched.WriteTo(writer);
    }

    private static bool JsonElementEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
            return false;

        switch (a.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                return true;

            case JsonValueKind.Number:
                return a.GetRawText() == b.GetRawText();

            case JsonValueKind.String:
                return a.GetString() == b.GetString();

            case JsonValueKind.Array:
                return ArrayEquals(a, b);

            case JsonValueKind.Object:
                return ObjectEquals(a, b);

            default:
                return a.GetRawText() == b.GetRawText();
        }
    }

    private static bool ArrayEquals(JsonElement a, JsonElement b)
    {
        var aLen = a.GetArrayLength();
        var bLen = b.GetArrayLength();
        if (aLen != bLen)
            return false;

        var aEnum = a.EnumerateArray();
        var bEnum = b.EnumerateArray();

        while (aEnum.MoveNext() && bEnum.MoveNext())
        {
            if (!JsonElementEquals(aEnum.Current, bEnum.Current))
                return false;
        }
        return true;
    }

    private static bool ObjectEquals(JsonElement a, JsonElement b)
    {
        var aProps = new Dictionary<string, JsonElement>();
        foreach (var prop in a.EnumerateObject())
            aProps[prop.Name] = prop.Value;

        var bCount = 0;
        foreach (var prop in b.EnumerateObject())
        {
            bCount++;
            if (!aProps.TryGetValue(prop.Name, out var aVal) || !JsonElementEquals(aVal, prop.Value))
                return false;
        }

        return aProps.Count == bCount;
    }
}
