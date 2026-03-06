using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class NestedObjectTests
{
    [Fact]
    public void NestedObject_CreatesAddAndReplaceOperations()
    {
        var json = """{"subModel": {"value1": "hello", "value2": 42}}""";
        var patch = PatchBuilder<NestedModel>.Build(json);

        // Should have: Add /subModel, Replace /subModel/value1, Replace /subModel/value2
        Assert.Equal(3, patch.Operations.Count);

        var addOp = patch.Operations.First(o => o.path == "/subModel");
        Assert.Equal(MergePatchOperationType.Add, addOp.OperationType);

        var v1Op = patch.Operations.First(o => o.path == "/subModel/value1");
        Assert.Equal(MergePatchOperationType.Replace, v1Op.OperationType);
        Assert.Equal("hello", v1Op.value);

        var v2Op = patch.Operations.First(o => o.path == "/subModel/value2");
        Assert.Equal(MergePatchOperationType.Replace, v2Op.OperationType);
        Assert.Equal(42, v2Op.value);
    }

    [Fact]
    public void NestedObject_ApplyTo_CreatesSubObject()
    {
        var json = """{"subModel": {"value1": "created"}}""";
        var patch = PatchBuilder<NestedModel>.Build(json);
        var target = new NestedModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.NotNull(target.SubModel);
        Assert.Equal("created", target.SubModel!.Value1);
    }

    [Fact]
    public void NestedObject_ApplyTo_UpdatesExisting()
    {
        var json = """{"subModel": {"value1": "updated"}}""";
        var patch = PatchBuilder<NestedModel>.Build(json);
        var target = new NestedModel
        {
            Id = 1,
            SubModel = new SubModel { Value1 = "old", Value2 = 10 }
        };

        patch.ApplyTo(target);

        Assert.Equal("updated", target.SubModel!.Value1);
        Assert.Equal(10, target.SubModel.Value2); // unchanged
    }

    [Fact]
    public void NestedObject_NullValue_RemovesSubObject()
    {
        var json = """{"subModel": null}""";
        var patch = PatchBuilder<NestedModel>.Build(json);
        var target = new NestedModel
        {
            SubModel = new SubModel { Value1 = "existing" }
        };

        patch.ApplyTo(target);

        Assert.Null(target.SubModel);
    }

    [Fact]
    public void NestedObject_PartialUpdate_OnlyChangedFields()
    {
        var json = """{"subModel": {"value2": 99}}""";
        var patch = PatchBuilder<NestedModel>.Build(json);
        var target = new NestedModel
        {
            SubModel = new SubModel { Value1 = "keep", Value2 = 1 }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.SubModel!.Value1);
        Assert.Equal(99, target.SubModel.Value2);
    }

    [Fact]
    public void NestedObject_ProvidedFields_IncludesNestedPaths()
    {
        var json = """{"subModel": {"value1": "test"}}""";
        var patch = PatchBuilder<NestedModel>.Build(json);

        var paths = patch.Operations.Select(o => o.path).ToList();
        Assert.Contains("/subModel", paths);
        Assert.Contains("/subModel/value1", paths);
    }
}
