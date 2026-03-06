using Xunit;

namespace SystemTextJsonMergePatch.Tests;

public class InputFormatterTests
{
    [Fact]
    public void Formatter_SupportsMergePatchContentType()
    {
        var formatter = new JsonMergePatchInputFormatter();

        Assert.Contains(JsonMergePatchDocument.ContentType, formatter.SupportedMediaTypes);
    }

    [Fact]
    public void ContentType_ConstantValue()
    {
        Assert.Equal("application/merge-patch+json", JsonMergePatchDocument.ContentType);
    }
}
