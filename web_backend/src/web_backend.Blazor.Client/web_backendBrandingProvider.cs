using Microsoft.Extensions.Localization;
using web_backend.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace web_backend.Blazor.Client;

[Dependency(ReplaceServices = true)]
public class web_backendBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<web_backendResource> _localizer;

    public web_backendBrandingProvider(IStringLocalizer<web_backendResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
