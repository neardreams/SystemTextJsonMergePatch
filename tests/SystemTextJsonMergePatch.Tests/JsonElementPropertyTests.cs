using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class JsonElementPropertyTests
{
    [Fact]
    public void Build_JsonElementProperty_CreatesReplaceOperation()
    {
        var json = """{"config": {"triggers": [1, 2, 3]}}""";
        var patch = PatchBuilder<JsonElementModel>.Build(json);

        var configOp = patch.Operations.First(o => o.path == "/config");
        Assert.Equal(MergePatchOperationType.Replace, configOp.OperationType);
    }

    [Fact]
    public void ApplyTo_JsonElementProperty_SetsValue()
    {
        var json = """{"config": {"triggers": [1, 2, 3]}}""";
        var patch = PatchBuilder<JsonElementModel>.Build(json);
        var target = new JsonElementModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.NotNull(target.Config);
        Assert.Equal(JsonValueKind.Object, target.Config!.Value.ValueKind);
        Assert.Equal(3, target.Config.Value.GetProperty("triggers").GetArrayLength());
    }

    [Fact]
    public void ApplyTo_JsonElementProperty_NullRemovesValue()
    {
        var json = """{"config": null}""";
        var patch = PatchBuilder<JsonElementModel>.Build(json);
        var target = new JsonElementModel
        {
            Id = 1,
            Config = JsonSerializer.Deserialize<JsonElement>("""{"old": true}""")
        };

        patch.ApplyTo(target);

        Assert.Null(target.Config);
    }

    [Fact]
    public void ApplyTo_JsonElementProperty_ReplacesExisting()
    {
        var json = """{"config": {"new": "data"}}""";
        var patch = PatchBuilder<JsonElementModel>.Build(json);
        var target = new JsonElementModel
        {
            Id = 1,
            Config = JsonSerializer.Deserialize<JsonElement>("""{"old": "data"}""")
        };

        patch.ApplyTo(target);

        Assert.NotNull(target.Config);
        Assert.True(target.Config!.Value.TryGetProperty("new", out _));
    }
}
