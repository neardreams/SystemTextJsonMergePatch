using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class ApplyToTests
{
    [Fact]
    public void ApplyTo_SetsStringProperty()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "NewName" });
        var target = new SimpleModel { Id = 1, Name = "OldName" };

        var result = patch.ApplyTo(target);

        Assert.Same(target, result);
        Assert.Equal("NewName", result.Name);
        Assert.Equal(1, result.Id); // unchanged
    }

    [Fact]
    public void ApplyTo_SetsBoolProperty()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { isEnabled = true });
        var target = new SimpleModel { IsEnabled = false };

        patch.ApplyTo(target);

        Assert.True(target.IsEnabled);
    }

    [Fact]
    public void ApplyTo_SetsIntProperty()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { displayOrder = 5 });
        var target = new SimpleModel { DisplayOrder = 0 };

        patch.ApplyTo(target);

        Assert.Equal(5, target.DisplayOrder);
    }

    [Fact]
    public void ApplyTo_RemoveOperation_SetsNull()
    {
        var json = """{"name": null}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);
        var target = new SimpleModel { Name = "Existing" };

        patch.ApplyTo(target);

        Assert.Null(target.Name);
    }

    [Fact]
    public void ApplyTo_MultipleOperations()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "New", description = "Desc", isEnabled = true });
        var target = new SimpleModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.Equal("New", target.Name);
        Assert.Equal("Desc", target.Description);
        Assert.True(target.IsEnabled);
        Assert.Equal(1, target.Id); // unchanged
    }

    [Fact]
    public void ApplyTo_CanOnlyBeCalledOnce()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Test" });
        var target = new SimpleModel();

        patch.ApplyTo(target);

        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(new SimpleModel()));
    }

    [Fact]
    public void ApplyTo_UnknownProperty_Ignored()
    {
        var json = """{"nonExistent": "value", "name": "Valid"}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);
        var target = new SimpleModel();

        patch.ApplyTo(target);

        Assert.Equal("Valid", target.Name);
    }

    // === ApplyToT ===

    [Fact]
    public void ApplyToT_CrossTypeApplication()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Cross", isEnabled = true });
        var target = new OtherModel { Id = 99 };

        var result = patch.ApplyToT(target);

        Assert.Same(target, result);
        Assert.Equal("Cross", result.Name);
        Assert.True(result.IsEnabled);
        Assert.Equal(99, result.Id);
    }

    [Fact]
    public void ApplyToT_DoesNotThrowOnMissingProperties()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { displayOrder = 5 });
        var target = new OtherModel { Id = 1 };

        // OtherModel doesn't have DisplayOrder — should just skip
        var result = patch.ApplyToT(target);

        Assert.Equal(1, result.Id);
    }
}
