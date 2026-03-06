using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SystemTextJsonMergePatch;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddJsonMergePatch(this IMvcBuilder builder, Action<JsonMergePatchOptions>? configure = null)
    {
        RegisterServices(builder.Services, configure);
        return builder;
    }

    public static IMvcCoreBuilder AddJsonMergePatch(this IMvcCoreBuilder builder, Action<JsonMergePatchOptions>? configure = null)
    {
        RegisterServices(builder.Services, configure);
        return builder;
    }

    private static void RegisterServices(IServiceCollection services, Action<JsonMergePatchOptions>? configure)
    {
        var patchOptions = new JsonMergePatchOptions();
        configure?.Invoke(patchOptions);

        services.AddSingleton(patchOptions);
        services.AddSingleton<IConfigureOptions<MvcOptions>, JsonMergePatchOptionsSetup>();
    }
}
