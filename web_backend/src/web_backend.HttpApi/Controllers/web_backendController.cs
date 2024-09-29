using web_backend.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace web_backend.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class web_backendController : AbpControllerBase
{
    protected web_backendController()
    {
        LocalizationResource = typeof(web_backendResource);
    }
}
