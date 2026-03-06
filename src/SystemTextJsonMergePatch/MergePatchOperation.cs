namespace SystemTextJsonMergePatch;

public sealed class MergePatchOperation
{
    public MergePatchOperation(string path, object? value, MergePatchOperationType operationType)
    {
        this.path = path;
        this.value = value;
        OperationType = operationType;
    }

    /// <summary>
    /// JSON Pointer path (e.g. "/propertyName" or "/nested/child").
    /// Lowercase property name to maintain API compatibility with Microsoft.AspNetCore.JsonPatch.Operations.Operation.
    /// </summary>
    public string path { get; }

    /// <summary>
    /// Deserialized CLR value. null for Remove operations.
    /// Lowercase property name to maintain API compatibility with Microsoft.AspNetCore.JsonPatch.Operations.Operation.
    /// </summary>
    public object? value { get; }

    public MergePatchOperationType OperationType { get; }
}
