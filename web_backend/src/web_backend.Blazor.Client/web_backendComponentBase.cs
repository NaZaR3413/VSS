using web_backend.Localization;
using Volo.Abp.AspNetCore.Components;

namespace web_backend.Blazor.Client;

public abstract class web_backendComponentBase : AbpComponentBase
{
    protected web_backendComponentBase()
    {
        LocalizationResource = typeof(web_backendResource);
    }
}
