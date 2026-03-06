using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class EnumTests
{
    [Fact]
    public void Build_EnumAsInt_CreatesReplaceOperation()
    {
        var json = """{"priority": 2}""";
        var patch = PatchBuilder<EnumModel>.Build(json);

        var op = patch.Operations.First(o => o.path == "/priority");
        Assert.Equal(MergePatchOperationType.Replace, op.OperationType);
    }

    [Fact]
    public void ApplyTo_EnumAsInt_SetsValue()
    {
        var json = """{"priority": 2}""";
        var patch = PatchBuilder<EnumModel>.Build(json);
        var target = new EnumModel { Priority = Priority.Low };

        patch.ApplyTo(target);

        Assert.Equal(Priority.High, target.Priority);
    }

    [Fact]
    public void ApplyTo_EnumNull_RemovesValue()
    {
        var json = """{"priority": null}""";
        var patch = PatchBuilder<EnumModel>.Build(json);
        var target = new EnumModel { Priority = Priority.High };

        patch.ApplyTo(target);

        Assert.Null(target.Priority);
    }

    [Fact]
    public void Build_EnumModel_ModelCorrectlyDeserialized()
    {
        var json = """{"id": 1, "priority": 3, "name": "Critical Item"}""";
        var patch = PatchBuilder<EnumModel>.Build(json);

        Assert.Equal(1, patch.Model.Id);
        Assert.Equal(Priority.Critical, patch.Model.Priority);
        Assert.Equal("Critical Item", patch.Model.Name);
    }
}
