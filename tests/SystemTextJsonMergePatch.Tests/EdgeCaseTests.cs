using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class EdgeCaseTests
{
    // === ApplyToT one-shot guard ===

    [Fact]
    public void ApplyToT_CanOnlyBeCalledOnce()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Test" });
        var target = new OtherModel();

        patch.ApplyToT(target);

        Assert.Throws<InvalidOperationException>(() => patch.ApplyToT(new OtherModel()));
    }

    [Fact]
    public void ApplyTo_Then_ApplyToT_Throws()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Test" });

        patch.ApplyTo(new SimpleModel());

        Assert.Throws<InvalidOperationException>(() => patch.ApplyToT(new OtherModel()));
    }

    [Fact]
    public void ApplyToT_Then_ApplyTo_Throws()
    {
        var patch = PatchBuilder<SimpleModel>.Build(new { name = "Test" });

        patch.ApplyToT(new OtherModel());

        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(new SimpleModel()));
    }

    // === Read-only property ===

    [Fact]
    public void Build_ReadOnlyProperty_IgnoredInOperations()
    {
        // ComputedField is getter-only — should not create operation for it
        var json = """{"name": "Test", "computedField": "ignored"}""";
        var patch = PatchBuilder<ReadOnlyModel>.Build(json);

        // Only "name" should have an operation; "computedField" is read-only and should be skipped
        var paths = patch.Operations.Select(o => o.path).ToList();
        Assert.Contains("/name", paths);
    }

    [Fact]
    public void ApplyTo_ReadOnlyProperty_DoesNotThrow()
    {
        var json = """{"name": "Applied", "computedField": "ignored"}""";
        var patch = PatchBuilder<ReadOnlyModel>.Build(json);
        var target = new ReadOnlyModel();

        // Should not throw even though "computedField" is in the JSON
        patch.ApplyTo(target);

        Assert.Equal("Applied", target.Name);
        Assert.Equal("Computed:Applied", target.ComputedField);
    }

    // === Deep nesting (3+ levels) ===

    [Fact]
    public void DeepNested_ThreeLevels_CreatesCorrectOperations()
    {
        var json = """{"level1": {"level2": {"value": "deep"}}}""";
        var patch = PatchBuilder<DeepNestedModel>.Build(json);

        var paths = patch.Operations.Select(o => o.path).ToList();
        Assert.Contains("/level1", paths);
        Assert.Contains("/level1/level2", paths);
        Assert.Contains("/level1/level2/value", paths);
    }

    [Fact]
    public void DeepNested_ThreeLevels_ApplyTo_CreatesAllLevels()
    {
        var json = """{"level1": {"level2": {"value": "deep", "number": 42}}}""";
        var patch = PatchBuilder<DeepNestedModel>.Build(json);
        var target = new DeepNestedModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.NotNull(target.Level1);
        Assert.NotNull(target.Level1!.Level2);
        Assert.Equal("deep", target.Level1.Level2!.Value);
        Assert.Equal(42, target.Level1.Level2.Number);
    }

    [Fact]
    public void DeepNested_PartialUpdate_PreservesOtherFields()
    {
        var json = """{"level1": {"level2": {"value": "updated"}}}""";
        var patch = PatchBuilder<DeepNestedModel>.Build(json);
        var target = new DeepNestedModel
        {
            Id = 1,
            Level1 = new Level1
            {
                Name = "keep",
                Level2 = new Level2 { Value = "old", Number = 99 }
            }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.Level1!.Name);
        Assert.Equal("updated", target.Level1.Level2!.Value);
        Assert.Equal(99, target.Level1.Level2.Number); // unchanged
    }

    // === Empty JSON object ===

    [Fact]
    public void EmptyObject_ProducesZeroOperations()
    {
        var json = """{}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);

        Assert.Empty(patch.Operations);
    }

    [Fact]
    public void EmptyObject_ApplyTo_LeavesTargetUnchanged()
    {
        var json = """{}""";
        var patch = PatchBuilder<SimpleModel>.Build(json);
        var target = new SimpleModel { Id = 1, Name = "Unchanged" };

        patch.ApplyTo(target);

        Assert.Equal(1, target.Id);
        Assert.Equal("Unchanged", target.Name);
    }

    // === DiffBuilder edge cases ===

    [Fact]
    public void DiffBuilder_BothSameArrays_NoDiff()
    {
        var a = new ArrayModel { Id = 1, Tags = new List<string> { "a", "b" } };
        var b = new ArrayModel { Id = 1, Tags = new List<string> { "a", "b" } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.Equal(0, diff.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void DiffBuilder_NullToValue_IncludesProperty()
    {
        var a = new NestedModel { Id = 1, SubModel = null };
        var b = new NestedModel { Id = 1, SubModel = new SubModel { Value1 = "new" } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("subModel", out var subEl));
        Assert.Equal(JsonValueKind.Object, subEl.ValueKind);
    }
}
