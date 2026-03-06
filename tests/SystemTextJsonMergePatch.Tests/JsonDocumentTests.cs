using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class JsonDocumentTests
{
    [Fact]
    public void Build_JsonDocumentProperty_CreatesReplaceOperation()
    {
        var json = """{"configuration": {"key": "value", "port": 502}}""";
        var patch = PatchBuilder<JsonDocumentModel>.Build(json);

        // JsonDocument is NOT complex — treated as leaf value, single Replace operation
        var configOp = patch.Operations.First(o => o.path == "/configuration");
        Assert.Equal(MergePatchOperationType.Replace, configOp.OperationType);
        Assert.IsType<JsonDocument>(configOp.value);
    }

    [Fact]
    public void ApplyTo_JsonDocumentProperty_SetsValue()
    {
        var json = """{"configuration": {"ipAddress": "192.168.1.1", "port": 502}}""";
        var patch = PatchBuilder<JsonDocumentModel>.Build(json);
        var target = new JsonDocumentModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.NotNull(target.Configuration);
        Assert.Equal("192.168.1.1", target.Configuration!.RootElement.GetProperty("ipAddress").GetString());
        Assert.Equal(502, target.Configuration.RootElement.GetProperty("port").GetInt32());
    }

    [Fact]
    public void ApplyTo_JsonDocumentProperty_NullRemovesValue()
    {
        var json = """{"configuration": null}""";
        var patch = PatchBuilder<JsonDocumentModel>.Build(json);

        using var existingDoc = JsonDocument.Parse("""{"old": true}""");
        var target = new JsonDocumentModel { Id = 1, Configuration = existingDoc };

        patch.ApplyTo(target);

        Assert.Null(target.Configuration);
    }

    [Fact]
    public void ApplyTo_JsonDocumentProperty_ReplacesExisting()
    {
        var json = """{"configuration": {"newKey": "newValue"}}""";
        var patch = PatchBuilder<JsonDocumentModel>.Build(json);

        using var existingDoc = JsonDocument.Parse("""{"oldKey": "oldValue"}""");
        var target = new JsonDocumentModel { Id = 1, Configuration = existingDoc };

        patch.ApplyTo(target);

        Assert.NotNull(target.Configuration);
        Assert.True(target.Configuration!.RootElement.TryGetProperty("newKey", out _));
        Assert.False(target.Configuration.RootElement.TryGetProperty("oldKey", out _));
    }

    [Fact]
    public void Build_JsonDocumentProperty_ModelHasCorrectValue()
    {
        var json = """{"id": 42, "configuration": {"test": true}}""";
        var patch = PatchBuilder<JsonDocumentModel>.Build(json);

        Assert.Equal(42, patch.Model.Id);
        Assert.NotNull(patch.Model.Configuration);
        Assert.True(patch.Model.Configuration!.RootElement.GetProperty("test").GetBoolean());
    }
}
