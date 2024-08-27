using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;

namespace WebApi_Templates.Models.Providers;

public class StringRequestCultureProvider : QueryStringRequestCultureProvider
{
    public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue("lang", out var lang))
        {
            return Task.FromResult(new ProviderCultureResult([new StringSegment(lang)]));
        }

        return Task.FromResult(new ProviderCultureResult([new StringSegment("zh-cn")]));
    }
}