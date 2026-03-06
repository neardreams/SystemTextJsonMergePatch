namespace SystemTextJsonMergePatch;

public class JsonMergePatchOptions
{
    /// <summary>
    /// When true, null values in the patch document will generate Remove operations
    /// that set the target property to null/default. Default is true.
    /// </summary>
    public bool EnableDelete { get; set; } = true;
}
