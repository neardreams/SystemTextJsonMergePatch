using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJsonMergePatch;

/// <summary>
/// Typed patch builder with 4 Build overloads matching Morcatko API.
/// </summary>
public static class PatchBuilder<T> where T : class
{
    /// <summary>
    /// Build from diff (original + patched objects).
    /// </summary>
    public static JsonMergePatchDocument<T> Build(T original, T patched, JsonMergePatchOptions? options = null)
    {
        options ??= new JsonMergePatchOptions();
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var originalJson = JsonSerializer.SerializeToElement(original, jsonOptions);
        var patchedJson = JsonSerializer.SerializeToElement(patched, jsonOptions);

        var diffElement = DiffBuilder.BuildElement(originalJson, patchedJson);
        var model = JsonSerializer.Deserialize<T>(diffElement.GetRawText(), jsonOptions)!;

        var operations = new List<MergePatchOperation>();
        PatchBuilderInternal.BuildOperations(diffElement, string.Empty, operations, typeof(T), options, jsonOptions);

        return new JsonMergePatchDocument<T>(model, operations, jsonOptions);
    }

    /// <summary>
    /// Build from a JSON string.
    /// </summary>
    public static JsonMergePatchDocument<T> Build(string json, JsonSerializerOptions? jsonOptions = null, JsonMergePatchOptions? options = null)
    {
        jsonOptions ??= new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options ??= new JsonMergePatchOptions();

        using var doc = JsonDocument.Parse(json);
        return BuildFromElement(doc.RootElement.Clone(), jsonOptions, options);
    }

    /// <summary>
    /// Build from an anonymous or typed object.
    /// </summary>
    public static JsonMergePatchDocument<T> Build(object jsonObjectPatch, JsonMergePatchOptions? options = null)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options ??= new JsonMergePatchOptions();

        var json = JsonSerializer.Serialize(jsonObjectPatch, jsonOptions);
        using var doc = JsonDocument.Parse(json);
        return BuildFromElement(doc.RootElement.Clone(), jsonOptions, options);
    }

    /// <summary>
    /// Build from a JsonElement.
    /// </summary>
    public static JsonMergePatchDocument<T> Build(JsonElement json, JsonMergePatchOptions? options = null)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options ??= new JsonMergePatchOptions();

        return BuildFromElement(json, jsonOptions, options);
    }

    private static JsonMergePatchDocument<T> BuildFromElement(JsonElement element, JsonSerializerOptions jsonOptions, JsonMergePatchOptions options)
    {
        var model = JsonSerializer.Deserialize<T>(element.GetRawText(), jsonOptions)!;

        var operations = new List<MergePatchOperation>();
        PatchBuilderInternal.BuildOperations(element, string.Empty, operations, typeof(T), options, jsonOptions);

        return new JsonMergePatchDocument<T>(model, operations, jsonOptions);
    }
}

/// <summary>
/// Internal helper for recursively building operations from a JSON element.
/// </summary>
internal static class PatchBuilderInternal
{
    internal static void BuildOperations(
        JsonElement element,
        string basePath,
        List<MergePatchOperation> operations,
        Type targetType,
        JsonMergePatchOptions options,
        JsonSerializerOptions jsonOptions)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in element.EnumerateObject())
        {
            var path = $"{basePath}/{prop.Name}";
            var clrProperty = ResolveClrProperty(targetType, prop.Name, jsonOptions);

            if (prop.Value.ValueKind == JsonValueKind.Null)
            {
                if (options.EnableDelete)
                {
                    operations.Add(new MergePatchOperation(path, null, MergePatchOperationType.Remove));
                }
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object && clrProperty != null && IsComplexType(clrProperty.PropertyType))
            {
                // Nested object: Add operation for the container, then recurse
                operations.Add(new MergePatchOperation(path, null, MergePatchOperationType.Add));
                BuildOperations(prop.Value, path, operations, clrProperty.PropertyType, options, jsonOptions);
            }
            else
            {
                // Value property: deserialize to CLR type
                object? value = DeserializeValue(prop.Value, clrProperty?.PropertyType, jsonOptions);
                operations.Add(new MergePatchOperation(path, value, MergePatchOperationType.Replace));
            }
        }
    }

    private static object? DeserializeValue(JsonElement element, Type? targetType, JsonSerializerOptions jsonOptions)
    {
        if (targetType == null)
            return element.Clone();

        // Deserialize to the target CLR type. No silent fallback — if deserialization fails,
        // let the exception propagate so the caller gets a clear error at build time
        // rather than a confusing error later during ApplyTo.
        return JsonSerializer.Deserialize(element.GetRawText(), targetType, jsonOptions);
    }

    private static bool IsComplexType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsPrimitive || underlying.IsEnum)
            return false;
        if (underlying == typeof(string) || underlying == typeof(decimal) ||
            underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) ||
            underlying == typeof(Guid) || underlying == typeof(TimeSpan))
            return false;
        if (underlying == typeof(JsonElement) || underlying == typeof(JsonDocument))
            return false;

        // Collections/arrays/dictionaries are NOT complex for recursion purposes — they are replaced wholesale
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string))
            return false;

        return true;
    }

    private static PropertyInfo? ResolveClrProperty(Type type, string jsonName, JsonSerializerOptions jsonOptions)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        // 1. [JsonPropertyName]
        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (attr != null && string.Equals(attr.Name, jsonName, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        // 2. JsonNamingPolicy
        var policy = jsonOptions.PropertyNamingPolicy;
        if (policy != null)
        {
            foreach (var prop in properties)
            {
                if (string.Equals(policy.ConvertName(prop.Name), jsonName, StringComparison.OrdinalIgnoreCase))
                    return prop;
            }
        }

        // 3. Case-insensitive
        foreach (var prop in properties)
        {
            if (string.Equals(prop.Name, jsonName, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        return null;
    }
}
