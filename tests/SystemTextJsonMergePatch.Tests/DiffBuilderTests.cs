using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class DiffBuilderTests
{
    [Fact]
    public void Build_NoDifferences_ReturnsEmptyObject()
    {
        var a = new SimpleModel { Id = 1, Name = "Same" };
        var b = new SimpleModel { Id = 1, Name = "Same" };

        using var diff = DiffBuilder.Build(a, b);
        var root = diff.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(0, root.EnumerateObject().Count());
    }

    [Fact]
    public void Build_StringDifference_IncludedInDiff()
    {
        var a = new SimpleModel { Id = 1, Name = "Old" };
        var b = new SimpleModel { Id = 1, Name = "New" };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("name", out var nameEl));
        Assert.Equal("New", nameEl.GetString());
        Assert.False(diff.RootElement.TryGetProperty("id", out _)); // unchanged
    }

    [Fact]
    public void Build_MultipleDifferences()
    {
        var a = new SimpleModel { Id = 1, Name = "A", Description = "D1", IsEnabled = true };
        var b = new SimpleModel { Id = 1, Name = "B", Description = "D2", IsEnabled = true };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("name", out _));
        Assert.True(diff.RootElement.TryGetProperty("description", out _));
        Assert.False(diff.RootElement.TryGetProperty("isEnabled", out _)); // same
    }

    [Fact]
    public void Build_NullToValue_IncludedInDiff()
    {
        var a = new SimpleModel { Id = 1, Name = null };
        var b = new SimpleModel { Id = 1, Name = "Added" };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("name", out var el));
        Assert.Equal("Added", el.GetString());
    }

    [Fact]
    public void Build_ValueToNull_IncludedAsNull()
    {
        var a = new SimpleModel { Id = 1, Name = "Exists" };
        var b = new SimpleModel { Id = 1, Name = null };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("name", out var el));
        Assert.Equal(JsonValueKind.Null, el.ValueKind);
    }

    [Fact]
    public void Build_NestedObject_RecursiveDiff()
    {
        var a = new NestedModel { Id = 1, SubModel = new SubModel { Value1 = "Old", Value2 = 10 } };
        var b = new NestedModel { Id = 1, SubModel = new SubModel { Value1 = "New", Value2 = 10 } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("subModel", out var subEl));
        Assert.True(subEl.TryGetProperty("value1", out var v1));
        Assert.Equal("New", v1.GetString());
        Assert.False(subEl.TryGetProperty("value2", out _)); // unchanged
    }

    [Fact]
    public void Build_ArrayDifference_ReplacesEntireArray()
    {
        var a = new ArrayModel { Id = 1, Tags = new List<string> { "a", "b" } };
        var b = new ArrayModel { Id = 1, Tags = new List<string> { "x", "y", "z" } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("tags", out var tagsEl));
        Assert.Equal(JsonValueKind.Array, tagsEl.ValueKind);
        Assert.Equal(3, tagsEl.GetArrayLength());
    }

    [Fact]
    public void Build_BoolDifference()
    {
        var a = new SimpleModel { IsEnabled = true };
        var b = new SimpleModel { IsEnabled = false };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("isEnabled", out var el));
        Assert.False(el.GetBoolean());
    }
}
