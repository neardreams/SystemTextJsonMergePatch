using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class PatchBuilderTests
{
    // === Build from anonymous object ===

    [Fact]
    public void Build_FromAnonymousObject_CreatesCorrectOperations()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { id = 1, name = "Test" });

        Assert.NotNull(patch.Model);
        Assert.Equal(1, patch.Model.Id);
        Assert.Equal("Test", patch.Model.Name);
        Assert.Equal(2, patch.Operations.Count);
    }

    [Fact]
    public void Build_FromAnonymousObject_ReplaceOperationType()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Test" });

        var op = Assert.Single(patch.Operations);
        Assert.Equal("/name", op.path);
        Assert.Equal("Test", op.value);
        Assert.Equal(MergePatchOperationType.Replace, op.OperationType);
    }

    // === Build from JSON string ===

    [Fact]
    public void Build_FromJsonString_CreatesCorrectOperations()
    {
        var json = """{"id": 1, "name": "FromString"}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);

        Assert.Equal(1, patch.Model.Id);
        Assert.Equal("FromString", patch.Model.Name);
        Assert.Equal(2, patch.Operations.Count);
    }

    [Fact]
    public void Build_FromJsonString_WithCustomOptions()
    {
        var json = """{"id": 1, "name": "Custom"}""";
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var patch = PatchBuilder<SimpleModel>.Build(json, jsonOptions);

        Assert.Equal("Custom", patch.Model.Name);
    }

    // === Build from JsonElement ===

    [Fact]
    public void Build_FromJsonElement_CreatesCorrectOperations()
    {
        var json = """{"id": 1, "name": "FromElement"}""";
        var element = JsonSerializer.Deserialize<JsonElement>(json);

        var patch = PatchBuilder<SimpleModel>.Build(element);

        Assert.Equal(1, patch.Model.Id);
        Assert.Equal("FromElement", patch.Model.Name);
    }

    // === Build from diff ===

    [Fact]
    public void Build_FromDiff_DetectsChanges()
    {
        var original = new SimpleModel { Id = 1, Name = "Original", Description = "Desc" };
        var patched = new SimpleModel { Id = 1, Name = "Changed", Description = "Desc" };

        var patch = PatchBuilder<SimpleModel>.Build(original, patched);

        // Only name changed
        Assert.Single(patch.Operations);
        Assert.Equal("/name", patch.Operations[0].path);
        Assert.Equal("Changed", patch.Operations[0].value);
    }

    [Fact]
    public void Build_FromDiff_NoChanges_EmptyOperations()
    {
        var original = new SimpleModel { Id = 1, Name = "Same" };
        var patched = new SimpleModel { Id = 1, Name = "Same" };

        var patch = PatchBuilder<SimpleModel>.Build(original, patched);

        Assert.Empty(patch.Operations);
    }

    // === Null handling ===

    [Fact]
    public void Build_NullValue_CreatesRemoveOperation()
    {
        var json = """{"name": null}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);

        var op = Assert.Single(patch.Operations);
        Assert.Equal("/name", op.path);
        Assert.Equal(MergePatchOperationType.Remove, op.OperationType);
        Assert.Null(op.value);
    }

    [Fact]
    public void Build_NullValue_EnableDeleteFalse_NoOperation()
    {
        var json = """{"name": null}""";
        var patch = PatchBuilder<SimpleModel>.Build(json, options: new JsonMergePatchOptions { EnableDelete = false });

        Assert.Empty(patch.Operations);
    }

    // === Multiple fields ===

    [Fact]
    public void Build_MultipleFields_AllTracked()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { id = 1, name = "N", description = "D", isEnabled = true });

        Assert.Equal(4, patch.Operations.Count);
        var paths = patch.Operations.Select(o => o.path).ToList();
        Assert.Contains("/id", paths);
        Assert.Contains("/name", paths);
        Assert.Contains("/description", paths);
        Assert.Contains("/isEnabled", paths);
    }

    // === Bool/numeric values ===

    [Fact]
    public void Build_BoolValue_CorrectType()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { isEnabled = false });

        var op = Assert.Single(patch.Operations);
        Assert.Equal(false, op.value);
    }

    [Fact]
    public void Build_IntValue_CorrectType()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { displayOrder = 42 });

        var op = Assert.Single(patch.Operations);
        Assert.Equal(42, op.value);
    }
}
