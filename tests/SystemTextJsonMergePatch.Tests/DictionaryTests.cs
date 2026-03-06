using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class DictionaryTests
{
    [Fact]
    public void Dictionary_ReplacedWholeSale()
    {
        var json = """{"properties": {"key1": "val1", "key2": "val2"}}""";
        var patch = PatchBuilder<DictionaryModel>.Build(json);

        // Dictionaries are collections, treated as wholesale replace
        var op = Assert.Single(patch.Operations);
        Assert.Equal("/properties", op.path);
        Assert.Equal(MergePatchOperationType.Replace, op.OperationType);
    }

    [Fact]
    public void Dictionary_ApplyTo_ReplacesEntireDictionary()
    {
        var json = """{"properties": {"new": "value"}}""";
        var patch = PatchBuilder<DictionaryModel>.Build(json);
        var target = new DictionaryModel
        {
            Properties = new Dictionary<string, string> { ["old"] = "data" }
        };

        patch.ApplyTo(target);

        Assert.Single(target.Properties);
        Assert.Equal("value", target.Properties["new"]);
    }

    [Fact]
    public void Dictionary_NullValue_EnableDelete_RemovesDictionary()
    {
        var json = """{"properties": null}""";
        var patch = PatchBuilder<DictionaryModel>.Build(json);
        var target = new DictionaryModel
        {
            Properties = new Dictionary<string, string> { ["key"] = "val" }
        };

        patch.ApplyTo(target);

        Assert.Null(target.Properties);
    }

    [Fact]
    public void Dictionary_EnableDeleteFalse_NullIgnored()
    {
        var json = """{"properties": null}""";
        var options = new JsonMergePatchOptions { EnableDelete = false };
        var patch = PatchBuilder<DictionaryModel>.Build(json, options: options);

        Assert.Empty(patch.Operations);
    }
}
