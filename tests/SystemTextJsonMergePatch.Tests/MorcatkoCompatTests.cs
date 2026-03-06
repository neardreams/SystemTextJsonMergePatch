// Test cases adapted from Morcatko.AspNetCore.JsonMergePatch
// Original: https://github.com/Morcatko/Morcatko.AspNetCore.JsonMergePatch
// MIT License, Copyright (c) 2018 Ondrej Morsky

using System.Text.Json;
using Xunit;

namespace SystemTextJsonMergePatch.Tests;

/// <summary>
/// Tests ported from Morcatko.AspNetCore.JsonMergePatch to verify API and behavioral compatibility.
/// Covers: PatchTest, PatchOtherTypeTest, PropertyRemovalTest, Attributes,
/// Diff (Flat/Nested/Array/Dictionary), PatchBuilderTests.
/// </summary>
public class MorcatkoCompatTests
{
    // =============================================
    // PatchTest — Core patch operations
    // =============================================

    [Fact]
    public void PatchInteger()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { integer = 42 });
        var target = new MorcatkoTestModel { Integer = 0 };

        patch.ApplyTo(target);

        Assert.Equal(42, target.Integer);
    }

    [Fact]
    public void PatchFloat()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { @float = 3.14f });
        var target = new MorcatkoTestModel { Float = 0 };

        patch.ApplyTo(target);

        Assert.Equal(3.14f, target.Float);
    }

    [Fact]
    public void PatchBoolean()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { boolean = true });
        var target = new MorcatkoTestModel { Boolean = false };

        patch.ApplyTo(target);

        Assert.True(target.Boolean);
    }

    [Fact]
    public void PatchString()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { @string = "patched" });
        var target = new MorcatkoTestModel { String = "original" };

        patch.ApplyTo(target);

        Assert.Equal("patched", target.String);
    }

    [Fact]
    public void PatchSimpleEnum()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { simpleEnum = SimpleEnum.Two });
        var target = new MorcatkoTestModel { SimpleEnum = SimpleEnum.Zero };

        patch.ApplyTo(target);

        Assert.Equal(SimpleEnum.Two, target.SimpleEnum);
    }

    [Fact]
    public void PatchDateTimeOffset()
    {
        var date = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var json = $$"""{"date": "{{date:O}}"}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel();

        patch.ApplyTo(target);

        Assert.NotNull(target.Date);
        Assert.Equal(date, target.Date);
    }

    [Fact]
    public void PatchNullableDecimal()
    {
        var json = """{"nullableDecimal": 99.99}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel { NullableDecimal = 0 };

        patch.ApplyTo(target);

        Assert.Equal(99.99m, target.NullableDecimal);
    }

    [Fact]
    public void PatchNullableDecimal_SetToNull()
    {
        var json = """{"nullableDecimal": null}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel { NullableDecimal = 42 };

        patch.ApplyTo(target);

        Assert.Null(target.NullableDecimal);
    }

    [Fact]
    public void PatchMultipleProperties()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(
            new { integer = 42, @string = "hello", boolean = true });
        var target = new MorcatkoTestModel();

        patch.ApplyTo(target);

        Assert.Equal(42, target.Integer);
        Assert.Equal("hello", target.String);
        Assert.True(target.Boolean);
    }

    // --- Sub-object patching ---

    [Fact]
    public void PatchSubProperty()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { subModel = new { value1 = "patched" } });
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "original", Value2 = "keep" }
        };

        patch.ApplyTo(target);

        Assert.Equal("patched", target.SubModel!.Value1);
        Assert.Equal("keep", target.SubModel.Value2);
    }

    [Fact]
    public void PatchSubPropertyOfNotExistingObject()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { subModel = new { value1 = "new" } });
        var target = new MorcatkoTestModel { SubModel = null };

        patch.ApplyTo(target);

        Assert.NotNull(target.SubModel);
        Assert.Equal("new", target.SubModel!.Value1);
    }

    [Fact]
    public void PatchTwoSubPropertiesOfNotExistingObject()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(new { subModel = new { value1 = "v1", value2 = "v2" } });
        var target = new MorcatkoTestModel { SubModel = null };

        patch.ApplyTo(target);

        Assert.NotNull(target.SubModel);
        Assert.Equal("v1", target.SubModel!.Value1);
        Assert.Equal("v2", target.SubModel.Value2);
    }

    [Fact]
    public void PatchAddAnObjectToANullProperty()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(
            new { subModel = new { value1 = "added", value2 = "also" } });
        var target = new MorcatkoTestModel();

        patch.ApplyTo(target);

        Assert.NotNull(target.SubModel);
        Assert.Equal("added", target.SubModel!.Value1);
        Assert.Equal("also", target.SubModel.Value2);
    }

    // --- Sub-sub-object patching ---

    [Fact]
    public void PatchSubSubObjectEmpty()
    {
        var json = """{"subModel": {"subSubModel": {}}}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel
            {
                Value1 = "keep",
                SubSubModel = new MorcatkoSubSubModel { Value1 = "keepSub" }
            }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.SubModel!.Value1);
        Assert.NotNull(target.SubModel.SubSubModel);
        Assert.Equal("keepSub", target.SubModel.SubSubModel!.Value1);
    }

    [Fact]
    public void PatchRemoveSubSubObject()
    {
        var json = """{"subModel": {"subSubModel": null}}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel
            {
                Value1 = "keep",
                SubSubModel = new MorcatkoSubSubModel { Value1 = "remove" }
            }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.SubModel!.Value1);
        Assert.Null(target.SubModel.SubSubModel);
    }

    [Fact]
    public void PatchSubSubObjectProperty()
    {
        var json = """{"subModel": {"subSubModel": {"value1": "patched"}}}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel
            {
                Value1 = "keep",
                SubSubModel = new MorcatkoSubSubModel { Value1 = "original" }
            }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.SubModel!.Value1);
        Assert.Equal("patched", target.SubModel.SubSubModel!.Value1);
    }

    // --- Object-level operations ---

    [Fact]
    public void PatchObject_FullReplace()
    {
        var patch = PatchBuilder<MorcatkoTestModel>.Build(
            new { subModel = new { value1 = "new1", value2 = "new2" } });
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "old1", Value2 = "old2" }
        };

        patch.ApplyTo(target);

        Assert.Equal("new1", target.SubModel!.Value1);
        Assert.Equal("new2", target.SubModel.Value2);
    }

    [Fact]
    public void NullObject()
    {
        var json = """{"subModel": null}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "existing" }
        };

        patch.ApplyTo(target);

        Assert.Null(target.SubModel);
    }

    // --- Array patching ---

    [Fact]
    public void PatchArrayOfFloats()
    {
        var json = """{"arrayOfFloats": [1.1, 2.2, 3.3]}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel { ArrayOfFloats = Array.Empty<float>() };

        patch.ApplyTo(target);

        Assert.NotNull(target.ArrayOfFloats);
        Assert.Equal(3, target.ArrayOfFloats!.Length);
        Assert.Equal(1.1f, target.ArrayOfFloats[0]);
        Assert.Equal(2.2f, target.ArrayOfFloats[1]);
        Assert.Equal(3.3f, target.ArrayOfFloats[2]);
    }

    [Fact]
    public void PatchSubModelNumbers()
    {
        var json = """{"subModel": {"numbers": [10, 20, 30]}}""";
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json);
        var target = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "keep", Numbers = new[] { 1, 2 } }
        };

        patch.ApplyTo(target);

        Assert.Equal("keep", target.SubModel!.Value1);
        Assert.Equal(new[] { 10, 20, 30 }, target.SubModel.Numbers);
    }

    // =============================================
    // PatchOtherTypeTest — Cross-type patching
    // =============================================

    [Fact]
    public void PatchesDifferentType()
    {
        var patch = PatchBuilder<TestPatchDto>.Build(new { name = "Test", age = 30 });
        var target = new TestTargetModel { Name = "Old", Age = 0 };

        patch.ApplyToT(target);

        Assert.Equal("Test", target.Name);
        Assert.Equal(30, target.Age);
    }

    [Fact]
    public void PatchesDifferentType_ThenApplyToThrows()
    {
        var patch = PatchBuilder<TestPatchDto>.Build(new { name = "Test" });

        patch.ApplyToT(new TestTargetModel());

        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(new TestPatchDto()));
    }

    // =============================================
    // PropertyRemovalTest
    // =============================================

    [Fact]
    public void PropertyRemoval_EnableDelete_True()
    {
        var json = """{"string": null}""";
        var options = new JsonMergePatchOptions { EnableDelete = true };
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json, options: options);
        var target = new MorcatkoTestModel { String = "existing" };

        patch.ApplyTo(target);

        Assert.Null(target.String);
    }

    [Fact]
    public void PropertyRemoval_EnableDelete_False_NoRemove()
    {
        var json = """{"string": null}""";
        var options = new JsonMergePatchOptions { EnableDelete = false };
        var patch = PatchBuilder<MorcatkoTestModel>.Build(json, options: options);
        var target = new MorcatkoTestModel { String = "existing" };

        patch.ApplyTo(target);

        Assert.Equal("existing", target.String);
    }

    // =============================================
    // Attributes — [JsonPropertyName] support
    // =============================================

    [Fact]
    public void Attributes_CanPatchWithJsonPropertyName()
    {
        var json = """{"custom_name": "hello", "is_active": true}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);
        var target = new JsonPropertyModel { Id = 1 };

        patch.ApplyTo(target);

        Assert.Equal("hello", target.CustomName);
        Assert.True(target.IsActive);
    }

    [Fact]
    public void Attributes_ModelDeserializesCorrectly()
    {
        var json = """{"id": 5, "custom_name": "test", "is_active": false}""";
        var patch = PatchBuilder<JsonPropertyModel>.Build(json);

        Assert.Equal(5, patch.Model.Id);
        Assert.Equal("test", patch.Model.CustomName);
        Assert.False(patch.Model.IsActive);
    }

    // =============================================
    // Build from diff (original, patched)
    // =============================================

    [Fact]
    public void Build_FromDiff_CreatesCorrectPatch()
    {
        var original = new MorcatkoTestModel { Integer = 1, String = "old", Boolean = false };
        var patched = new MorcatkoTestModel { Integer = 2, String = "old", Boolean = false };

        var patch = PatchBuilder<MorcatkoTestModel>.Build(original, patched);

        var intOp = patch.Operations.FirstOrDefault(o => o.path == "/integer");
        Assert.NotNull(intOp);
        Assert.Equal(MergePatchOperationType.Replace, intOp!.OperationType);
    }

    [Fact]
    public void Build_FromDiff_AppliesCorrectly()
    {
        var original = new MorcatkoTestModel { Integer = 1, String = "old" };
        var patched = new MorcatkoTestModel { Integer = 1, String = "new" };

        var patch = PatchBuilder<MorcatkoTestModel>.Build(original, patched);
        var target = new MorcatkoTestModel { Integer = 1, String = "old", Boolean = true };

        patch.ApplyTo(target);

        Assert.Equal("new", target.String);
        Assert.True(target.Boolean); // Not in diff, preserved
    }

    // =============================================
    // PatchBuilderTests — Complex scenarios
    // =============================================

    [Fact]
    public void PatchNewsletter_CrossType_WithNullRemoval()
    {
        var patch = PatchBuilder<TestPatchDto>.Build(new { name = "Updated", surname = (string?)null });

        var target = new TestTargetModel { Name = "Old", Surname = "OldSurname", Age = 25 };
        patch.ApplyToT(target);

        Assert.Equal("Updated", target.Name);
        Assert.Null(target.Surname);
        Assert.Equal(25, target.Age);
    }

    [Fact]
    public void Build_FromJsonStringWithArraysOfObjects()
    {
        var json = """
        {
            "quantity": 5,
            "models2": [
                {
                    "title": "First",
                    "models3": [
                        { "price": 9.99, "values": [1, 2, 3] }
                    ]
                },
                {
                    "title": "Second",
                    "models3": []
                }
            ]
        }
        """;

        var patch = PatchBuilder<ObjectArrayTestModel>.Build(json);

        Assert.Equal(5, patch.Model.Quantity);
        Assert.NotNull(patch.Model.Models2);
        Assert.Equal(2, patch.Model.Models2!.Count);
        Assert.Equal("First", patch.Model.Models2[0].Title);
        Assert.NotNull(patch.Model.Models2[0].Models3);
        Assert.Single(patch.Model.Models2[0].Models3!);
        Assert.Equal(9.99m, patch.Model.Models2[0].Models3![0].Price);
        Assert.Equal(new[] { 1, 2, 3 }, patch.Model.Models2[0].Models3![0].Values);
        Assert.Equal("Second", patch.Model.Models2[1].Title);
        Assert.Empty(patch.Model.Models2[1].Models3!);
    }

    [Fact]
    public void Build_FromJsonStringWithArraysOfObjects_ApplyTo()
    {
        var json = """
        {
            "quantity": 10,
            "models2": [
                {
                    "title": "Replaced",
                    "models3": [
                        { "price": 19.99, "values": [4, 5] }
                    ]
                }
            ]
        }
        """;

        var patch = PatchBuilder<ObjectArrayTestModel>.Build(json);
        var target = new ObjectArrayTestModel
        {
            Quantity = 1,
            Models2 = new List<ObjectArrayTestModel2>
            {
                new() { Title = "Old", Models3 = Array.Empty<ObjectArrayTestModel3>() }
            }
        };

        patch.ApplyTo(target);

        Assert.Equal(10, target.Quantity);
        Assert.NotNull(target.Models2);
        Assert.Single(target.Models2!);
        Assert.Equal("Replaced", target.Models2[0].Title);
    }

    // =============================================
    // Diff/Flat — Flat object diffs
    // =============================================

    [Fact]
    public void Diff_Flat_NoChange()
    {
        var a = new MorcatkoTestModel { Integer = 1, String = "test", Boolean = true };
        var b = new MorcatkoTestModel { Integer = 1, String = "test", Boolean = true };

        using var diff = DiffBuilder.Build(a, b);

        Assert.Equal(0, diff.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void Diff_Flat_NullToValue()
    {
        var a = new MorcatkoTestModel { String = null };
        var b = new MorcatkoTestModel { String = "hello" };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("string", out var val));
        Assert.Equal("hello", val.GetString());
    }

    [Fact]
    public void Diff_Flat_ValueToNull()
    {
        var a = new MorcatkoTestModel { String = "hello" };
        var b = new MorcatkoTestModel { String = null };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("string", out var val));
        Assert.Equal(JsonValueKind.Null, val.ValueKind);
    }

    [Fact]
    public void Diff_Flat_MultipleChanges()
    {
        var a = new MorcatkoTestModel { Integer = 1, String = "old", Boolean = false };
        var b = new MorcatkoTestModel { Integer = 2, String = "new", Boolean = true };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("integer", out var intVal));
        Assert.Equal(2, intVal.GetInt32());
        Assert.True(diff.RootElement.TryGetProperty("string", out var strVal));
        Assert.Equal("new", strVal.GetString());
        Assert.True(diff.RootElement.TryGetProperty("boolean", out var boolVal));
        Assert.True(boolVal.GetBoolean());
    }

    // =============================================
    // Diff/Nested — Nested object diffs
    // =============================================

    [Fact]
    public void Diff_Nested_Deep()
    {
        var a = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "old", Value2 = "keep" }
        };
        var b = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "new", Value2 = "keep" }
        };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("subModel", out var sub));
        Assert.True(sub.TryGetProperty("value1", out var v1));
        Assert.Equal("new", v1.GetString());
        Assert.False(sub.TryGetProperty("value2", out _));
    }

    [Fact]
    public void Diff_Nested_ObjectToNull()
    {
        var a = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "exists" }
        };
        var b = new MorcatkoTestModel { SubModel = null };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("subModel", out var sub));
        Assert.Equal(JsonValueKind.Null, sub.ValueKind);
    }

    [Fact]
    public void Diff_Nested_NullToObject()
    {
        var a = new MorcatkoTestModel { SubModel = null };
        var b = new MorcatkoTestModel
        {
            SubModel = new MorcatkoSubModel { Value1 = "created" }
        };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("subModel", out var sub));
        Assert.Equal(JsonValueKind.Object, sub.ValueKind);
        Assert.True(sub.TryGetProperty("value1", out var v1));
        Assert.Equal("created", v1.GetString());
    }

    // =============================================
    // Diff/Array — Array diffs
    // =============================================

    [Fact]
    public void Diff_Array_ValueChanged()
    {
        var a = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f, 2.0f } };
        var b = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f, 3.0f } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("arrayOfFloats", out var arr));
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(2, arr.GetArrayLength());
    }

    [Fact]
    public void Diff_Array_NullToValue()
    {
        var a = new MorcatkoTestModel { ArrayOfFloats = null };
        var b = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("arrayOfFloats", out var arr));
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public void Diff_Array_ValueToNull()
    {
        var a = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f } };
        var b = new MorcatkoTestModel { ArrayOfFloats = null };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("arrayOfFloats", out var arr));
        Assert.Equal(JsonValueKind.Null, arr.ValueKind);
    }

    [Fact]
    public void Diff_Array_SameContent_NoDiff()
    {
        var a = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f, 2.0f, 3.0f } };
        var b = new MorcatkoTestModel { ArrayOfFloats = new[] { 1.0f, 2.0f, 3.0f } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.False(diff.RootElement.TryGetProperty("arrayOfFloats", out _));
    }

    // =============================================
    // Diff/Dictionary
    // =============================================

    [Fact]
    public void Diff_Dictionary_KeyAdded()
    {
        var a = new DictionaryModel { Id = 1, Properties = new() { { "key1", "val1" } } };
        var b = new DictionaryModel { Id = 1, Properties = new() { { "key1", "val1" }, { "key2", "val2" } } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("properties", out _));
    }

    [Fact]
    public void Diff_Dictionary_KeyMissing()
    {
        var a = new DictionaryModel { Id = 1, Properties = new() { { "key1", "v" }, { "key2", "v" } } };
        var b = new DictionaryModel { Id = 1, Properties = new() { { "key1", "v" } } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("properties", out _));
    }

    [Fact]
    public void Diff_Dictionary_ValueChanged()
    {
        var a = new DictionaryModel { Id = 1, Properties = new() { { "key1", "old" } } };
        var b = new DictionaryModel { Id = 1, Properties = new() { { "key1", "new" } } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("properties", out _));
    }

    [Fact]
    public void Diff_Dictionary_NullToEmpty()
    {
        var a = new DictionaryModel { Id = 1, Properties = null };
        var b = new DictionaryModel { Id = 1, Properties = new() };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("properties", out var val));
        Assert.Equal(JsonValueKind.Object, val.ValueKind);
    }

    [Fact]
    public void Diff_Dictionary_EmptyToNull()
    {
        var a = new DictionaryModel { Id = 1, Properties = new() };
        var b = new DictionaryModel { Id = 1, Properties = null };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("properties", out var val));
        Assert.Equal(JsonValueKind.Null, val.ValueKind);
    }

    [Fact]
    public void Diff_Dictionary_ScoreChanged()
    {
        var a = new DictionaryModel { Id = 1, Scores = new() { { "math", 90 } } };
        var b = new DictionaryModel { Id = 1, Scores = new() { { "math", 80 } } };

        using var diff = DiffBuilder.Build(a, b);

        Assert.True(diff.RootElement.TryGetProperty("scores", out _));
    }
}
