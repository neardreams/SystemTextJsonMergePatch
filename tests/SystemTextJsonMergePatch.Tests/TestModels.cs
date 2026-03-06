using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJsonMergePatch.Tests;

public class SimpleModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsEnabled { get; set; }
    public int? DisplayOrder { get; set; }
}

public class NestedModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public SubModel? SubModel { get; set; }
}

public class SubModel
{
    public string? Value1 { get; set; }
    public int? Value2 { get; set; }
}

public class ArrayModel
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
    public List<int>? Numbers { get; set; }
}

public class DictionaryModel
{
    public int Id { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
    public Dictionary<string, int>? Scores { get; set; }
}

public class JsonPropertyModel
{
    public int Id { get; set; }

    [JsonPropertyName("custom_name")]
    public string? CustomName { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}

public class NullableModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? Count { get; set; }
    public bool? Flag { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class JsonDocumentModel
{
    public int Id { get; set; }
    public JsonDocument? Configuration { get; set; }
}

public class OtherModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsEnabled { get; set; }
}

public class JsonElementModel
{
    public int Id { get; set; }
    public JsonElement? Config { get; set; }
}

public enum Priority { Low, Medium, High, Critical }

public class EnumModel
{
    public int Id { get; set; }
    public Priority? Priority { get; set; }
    public string? Name { get; set; }
}

public class ReadOnlyModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string ComputedField => $"Computed:{Name}";
}

public class DeepNestedModel
{
    public int Id { get; set; }
    public Level1? Level1 { get; set; }
}

public class Level1
{
    public string? Name { get; set; }
    public Level2? Level2 { get; set; }
}

public class Level2
{
    public string? Value { get; set; }
    public int? Number { get; set; }
}

// === Morcatko-equivalent models ===

public enum SimpleEnum { Zero = 0, One = 1, Two = 2 }

public class MorcatkoTestModel
{
    public int Id { get; set; }
    public int Integer { get; set; }
    public string? String { get; set; }
    public float Float { get; set; }
    public bool Boolean { get; set; }
    public string? Renamed { get; set; }
    public MorcatkoSubModel? SubModel { get; set; }
    public SimpleEnum SimpleEnum { get; set; }
    public DateTimeOffset? Date { get; set; }
    public decimal? NullableDecimal { get; set; }
    public float[]? ArrayOfFloats { get; set; } = Array.Empty<float>();
}

public class MorcatkoSubModel
{
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public int[]? Numbers { get; set; }
    public MorcatkoSubSubModel? SubSubModel { get; set; }
}

public class MorcatkoSubSubModel
{
    public string? Value1 { get; set; }
}

public class ObjectArrayTestModel
{
    public int Quantity { get; set; }
    public List<ObjectArrayTestModel2>? Models2 { get; set; } = new();
}

public class ObjectArrayTestModel2
{
    public string? Title { get; set; }
    public ObjectArrayTestModel3[]? Models3 { get; set; } = Array.Empty<ObjectArrayTestModel3>();
}

public class ObjectArrayTestModel3
{
    public decimal Price { get; set; }
    public int[]? Values { get; set; } = Array.Empty<int>();
}

public class TestPatchDto
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public int? Age { get; set; }
}

public class TestTargetModel
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public int Age { get; set; }
}
