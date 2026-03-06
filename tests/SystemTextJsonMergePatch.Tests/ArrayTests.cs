using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class ArrayTests
{
    [Fact]
    public void Array_ReplacedWholeSale_PerRfc7396()
    {
        var json = """{"tags": ["a", "b", "c"]}""";
        var patch = PatchBuilder<ArrayModel>.Build(json);

        var op = Assert.Single(patch.Operations);
        Assert.Equal("/tags", op.path);
        Assert.Equal(MergePatchOperationType.Replace, op.OperationType);
    }

    [Fact]
    public void Array_ApplyTo_ReplacesEntireArray()
    {
        var json = """{"tags": ["x", "y"]}""";
        var patch = PatchBuilder<ArrayModel>.Build(json);
        var target = new ArrayModel { Tags = new List<string> { "a", "b", "c" } };

        patch.ApplyTo(target);

        Assert.Equal(new[] { "x", "y" }, target.Tags);
    }

    [Fact]
    public void Array_NullValue_RemovesArray()
    {
        var json = """{"tags": null}""";
        var patch = PatchBuilder<ArrayModel>.Build(json);
        var target = new ArrayModel { Tags = new List<string> { "a" } };

        patch.ApplyTo(target);

        Assert.Null(target.Tags);
    }

    [Fact]
    public void Array_EmptyArray_SetsEmpty()
    {
        var json = """{"tags": []}""";
        var patch = PatchBuilder<ArrayModel>.Build(json);
        var target = new ArrayModel { Tags = new List<string> { "a", "b" } };

        patch.ApplyTo(target);

        Assert.NotNull(target.Tags);
        Assert.Empty(target.Tags);
    }

    [Fact]
    public void IntArray_ReplacedWholeSale()
    {
        var json = """{"numbers": [1, 2, 3]}""";
        var patch = PatchBuilder<ArrayModel>.Build(json);
        var target = new ArrayModel { Numbers = new List<int> { 10, 20 } };

        patch.ApplyTo(target);

        Assert.Equal(new[] { 1, 2, 3 }, target.Numbers);
    }
}
