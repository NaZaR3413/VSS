using System;
using System.Collections.Generic;
using System.Text;
using web_backend.Localization;
using Volo.Abp.Application.Services;

namespace web_backend;

/* Inherit your application services from this class.
 */
public abstract class web_backendAppService : ApplicationService
{
    protected web_backendAppService()
    {
        LocalizationResource = typeof(web_backendResource);
    }
}
