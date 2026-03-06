using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJsonMergePatch;

public abstract class JsonMergePatchDocument
{
    public const string ContentType = "application/merge-patch+json";
}

public class JsonMergePatchDocument<T> : JsonMergePatchDocument where T : class
{
    private bool _applied;

    internal JsonMergePatchDocument(T model, List<MergePatchOperation> operations, JsonSerializerOptions? jsonOptions = null)
    {
        Model = model;
        Operations = operations;
        JsonOptions = jsonOptions;
    }

    public T Model { get; }
    public List<MergePatchOperation> Operations { get; }
    internal JsonSerializerOptions? JsonOptions { get; }

    /// <summary>
    /// Apply all operations to the target object. Mutates and returns the same instance.
    /// Can only be called once per document (matches Morcatko behavior).
    /// </summary>
    public T ApplyTo(T target)
    {
        if (_applied)
            throw new InvalidOperationException("ApplyTo can only be called once per JsonMergePatchDocument.");
        _applied = true;

        ApplyOperations(target, typeof(T));
        return target;
    }

    /// <summary>
    /// Apply operations to a different type that shares property names.
    /// Mutates and returns the same instance.
    /// Shares the one-shot guard with ApplyTo.
    /// </summary>
    public TOther ApplyToT<TOther>(TOther target) where TOther : class
    {
        if (_applied)
            throw new InvalidOperationException("ApplyTo/ApplyToT can only be called once per JsonMergePatchDocument.");
        _applied = true;

        ApplyOperations(target, typeof(TOther));
        return target;
    }

    private void ApplyOperations(object target, Type targetType)
    {
        foreach (var op in Operations)
        {
            var segments = op.path.TrimStart('/').Split('/');
            ApplyOperation(target, targetType, segments, 0, op);
        }
    }

    private void ApplyOperation(object target, Type targetType, string[] segments, int index, MergePatchOperation op)
    {
        var propertyName = segments[index];
        var property = ResolveProperty(targetType, propertyName);
        if (property == null)
            return;

        // Intermediate segment — navigate deeper
        if (index < segments.Length - 1)
        {
            var current = property.GetValue(target);
            if (current == null)
            {
                current = Activator.CreateInstance(property.PropertyType)!;
                property.SetValue(target, current);
            }
            ApplyOperation(current, property.PropertyType, segments, index + 1, op);
            return;
        }

        // Leaf segment
        switch (op.OperationType)
        {
            case MergePatchOperationType.Replace:
                SetPropertyValue(target, property, op.value);
                break;

            case MergePatchOperationType.Remove:
                SetPropertyDefault(target, property);
                break;

            case MergePatchOperationType.Add:
                var existing = property.GetValue(target);
                if (existing == null)
                {
                    var instance = Activator.CreateInstance(property.PropertyType);
                    property.SetValue(target, instance);
                }
                break;
        }
    }

    private static void SetPropertyValue(object target, PropertyInfo property, object? value)
    {
        if (value == null)
        {
            SetPropertyDefault(target, property);
            return;
        }

        var targetType = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (underlyingType != null)
            targetType = underlyingType;

        // If value is JsonElement, deserialize to target type
        if (value is JsonElement je)
        {
            var deserialized = JsonSerializer.Deserialize(je.GetRawText(), property.PropertyType);
            property.SetValue(target, deserialized);
            return;
        }

        // Direct assignment if compatible
        if (targetType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
            return;
        }

        // Convert
        try
        {
            var converted = Convert.ChangeType(value, targetType);
            property.SetValue(target, converted);
        }
        catch
        {
            // Fallback: serialize-then-deserialize
            var json = JsonSerializer.Serialize(value);
            var deserialized = JsonSerializer.Deserialize(json, property.PropertyType);
            property.SetValue(target, deserialized);
        }
    }

    private static void SetPropertyDefault(object target, PropertyInfo property)
    {
        var type = property.PropertyType;
        property.SetValue(target, type.IsValueType ? Activator.CreateInstance(type) : null);
    }

    /// <summary>
    /// Resolves a CLR property by JSON name. Checks [JsonPropertyName], then JsonNamingPolicy, then case-insensitive fallback.
    /// </summary>
    private PropertyInfo? ResolveProperty(Type type, string jsonName)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        // 1. Check [JsonPropertyName] attribute
        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (attr != null && string.Equals(attr.Name, jsonName, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        // 2. Check via JsonNamingPolicy
        var policy = JsonOptions?.PropertyNamingPolicy;
        if (policy != null)
        {
            foreach (var prop in properties)
            {
                if (string.Equals(policy.ConvertName(prop.Name), jsonName, StringComparison.OrdinalIgnoreCase))
                    return prop;
            }
        }

        // 3. Case-insensitive fallback
        foreach (var prop in properties)
        {
            if (string.Equals(prop.Name, jsonName, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        return null;
    }
}
