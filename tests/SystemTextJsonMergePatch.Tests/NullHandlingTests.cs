using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class NullHandlingTests
{
    [Fact]
    public void NullValue_GeneratesRemoveOperation()
    {
        var json = """{"name": null}""";
        var patch = PatchBuilder<NullableModel>.Build(json);

        var op = Assert.Single(patch.Operations);
        Assert.Equal(MergePatchOperationType.Remove, op.OperationType);
        Assert.Equal("/name", op.path);
    }

    [Fact]
    public void NullInt_GeneratesRemoveOperation()
    {
        var json = """{"count": null}""";
        var patch = PatchBuilder<NullableModel>.Build(json);

        var op = Assert.Single(patch.Operations);
        Assert.Equal(MergePatchOperationType.Remove, op.OperationType);
        Assert.Equal("/count", op.path);
    }

    [Fact]
    public void AbsentProperty_NoOperation()
    {
        var json = """{"name": "OnlyName"}""";
        var patch = PatchBuilder<NullableModel>.Build(json);

        Assert.Single(patch.Operations);
        Assert.Equal("/name", patch.Operations[0].path);
        Assert.Equal(MergePatchOperationType.Replace, patch.Operations[0].OperationType);
    }

    [Fact]
    public void NullVsAbsent_DistinguishedCorrectly()
    {
        // Only "name" is null, "count" is absent
        var json = """{"name": null}""";
        var patch = PatchBuilder<NullableModel>.Build(json);
        var target = new NullableModel { Name = "Old", Count = 42 };

        patch.ApplyTo(target);

        Assert.Null(target.Name);
        Assert.Equal(42, target.Count); // untouched
    }

    [Fact]
    public void NullBool_GeneratesRemoveOperation()
    {
        var json = """{"flag": null}""";
        var patch = PatchBuilder<NullableModel>.Build(json);

        var op = Assert.Single(patch.Operations);
        Assert.Equal(MergePatchOperationType.Remove, op.OperationType);
    }

    [Fact]
    public void NullBool_ApplyTo_SetsNull()
    {
        var json = """{"flag": null}""";
        var patch = PatchBuilder<NullableModel>.Build(json);
        var target = new NullableModel { Flag = true };

        patch.ApplyTo(target);

        Assert.Null(target.Flag);
    }

    [Fact]
    public void EnableDelete_False_NullIgnored()
    {
        var json = """{"name": null, "count": 5}""";
        var options = new JsonMergePatchOptions { EnableDelete = false };
        var patch = PatchBuilder<NullableModel>.Build(json, options: options);

        // Only count should have an operation
        Assert.Single(patch.Operations);
        Assert.Equal("/count", patch.Operations[0].path);
    }
}
