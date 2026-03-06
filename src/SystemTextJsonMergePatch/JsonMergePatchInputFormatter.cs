using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SystemTextJsonMergePatch;

public class JsonMergePatchInputFormatter : TextInputFormatter
{
    private readonly JsonMergePatchOptions _patchOptions;

    public JsonMergePatchInputFormatter(JsonMergePatchOptions? patchOptions = null)
    {
        _patchOptions = patchOptions ?? new JsonMergePatchOptions();

        SupportedMediaTypes.Add(JsonMergePatchDocument.ContentType);
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanReadType(Type type)
    {
        return IsJsonMergePatchType(type);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        var httpContext = context.HttpContext;
        var targetType = context.ModelType;

        using var reader = new StreamReader(httpContext.Request.Body, encoding);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
            return await InputFormatterResult.FailureAsync();

        try
        {
            if (IsGenericMergePatchType(targetType))
            {
                var modelType = targetType.GetGenericArguments()[0];
                var result = BuildPatchDocument(json, modelType);
                return await InputFormatterResult.SuccessAsync(result);
            }

            if (IsCollectionOfMergePatch(targetType, out var elementModelType))
            {
                var result = BuildPatchCollection(json, elementModelType!);
                return await InputFormatterResult.SuccessAsync(result);
            }

            return await InputFormatterResult.FailureAsync();
        }
        catch
        {
            return await InputFormatterResult.FailureAsync();
        }
    }

    private object BuildPatchDocument(string json, Type modelType)
    {
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement.Clone();

        var builderType = typeof(PatchBuilder<>).MakeGenericType(modelType);
        var buildMethod = builderType.GetMethod("Build", new[] { typeof(JsonElement), typeof(JsonMergePatchOptions) })!;
        return buildMethod.Invoke(null, new object?[] { element, _patchOptions })!;
    }

    private object BuildPatchCollection(string json, Type modelType)
    {
        using var doc = JsonDocument.Parse(json);
        var listType = typeof(List<>).MakeGenericType(typeof(JsonMergePatchDocument<>).MakeGenericType(modelType));
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var cloned = element.Clone();
            var builderType = typeof(PatchBuilder<>).MakeGenericType(modelType);
            var buildMethod = builderType.GetMethod("Build", new[] { typeof(JsonElement), typeof(JsonMergePatchOptions) })!;
            var patch = buildMethod.Invoke(null, new object?[] { cloned, _patchOptions })!;
            addMethod.Invoke(list, new[] { patch });
        }

        return list;
    }

    private static bool IsJsonMergePatchType(Type type)
    {
        return IsGenericMergePatchType(type) || IsCollectionOfMergePatch(type, out _);
    }

    private static bool IsGenericMergePatchType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonMergePatchDocument<>);
    }

    private static bool IsCollectionOfMergePatch(Type type, out Type? modelType)
    {
        modelType = null;

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = iface.GetGenericArguments()[0];
                if (IsGenericMergePatchType(elementType))
                {
                    modelType = elementType.GetGenericArguments()[0];
                    return true;
                }
            }
        }

        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            if (args.Length == 1 && IsGenericMergePatchType(args[0]))
            {
                modelType = args[0].GetGenericArguments()[0];
                return true;
            }
        }

        return false;
    }
}
