using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SystemTextJsonMergePatch;

internal class JsonMergePatchOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly JsonMergePatchOptions _patchOptions;

    public JsonMergePatchOptionsSetup(JsonMergePatchOptions patchOptions)
    {
        _patchOptions = patchOptions;
    }

    public void Configure(MvcOptions options)
    {
        var formatter = new JsonMergePatchInputFormatter(_patchOptions);
        options.InputFormatters.Insert(0, formatter);
    }
}
