using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class JsonPropertyNameTests
{
    [Fact]
    public void Build_JsonPropertyName_OperationUsesJsonName()
    {
        var json = """{"custom_name": "hello", "is_active": true}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);

        Assert.Equal(2, patch.Operations.Count);
        var paths = patch.Operations.Select(o => o.path).ToList();
        Assert.Contains("/custom_name", paths);
        Assert.Contains("/is_active", paths);
    }

    [Fact]
    public void Build_JsonPropertyName_ModelDeserializedCorrectly()
    {
        var json = """{"custom_name": "hello"}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);

        Assert.Equal("hello", patch.Model.CustomName);
    }

    [Fact]
    public void ApplyTo_JsonPropertyName_SetsPropertyViaJsonName()
    {
        var json = """{"custom_name": "applied", "is_active": false}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);
        var target = new JsonPropertyModel { CustomName = "old", IsActive = true };

        patch.ApplyTo(target);

        Assert.Equal("applied", target.CustomName);
        Assert.False(target.IsActive);
    }

    [Fact]
    public void ApplyTo_JsonPropertyName_NullRemovesValue()
    {
        var json = """{"custom_name": null}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);
        var target = new JsonPropertyModel { CustomName = "existing" };

        patch.ApplyTo(target);

        Assert.Null(target.CustomName);
    }

    [Fact]
    public void Build_FromAnonymousObject_JsonPropertyNameMapping()
    {
        // Anonymous objects use CamelCase serialization, not [JsonPropertyName]
        // So building from anonymous objects uses camelCase keys
        var patch = PatchBuilder<JsonPropertyModel>.Build(new { id = 1 });

        Assert.Single(patch.Operations);
        Assert.Equal("/id", patch.Operations[0].path);
    }

    [Fact]
    public void Build_FromJsonElement_JsonPropertyNameMapping()
    {
        var json = """{"custom_name": "test", "id": 5}""";
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var patch = PatchBuilder<JsonPropertyModel>.Build(element);

        Assert.Equal("test", patch.Model.CustomName);
        Assert.Equal(5, patch.Model.Id);
    }
}
