using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account.Web.Pages.Account;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace web_backend.HttpApi.Host.Pages.Account;

public class CustomLogoutModel : LogoutModel
{
    public override async Task<IActionResult> OnPostAsync()
    {
        await SignInManager.SignOutAsync();
        await HttpContext.SignOutAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        var cookieNames = new[]
        {
            ".AspNetCore.Identity.Application",
            ".AspNetCore.Identity.External",
            ".AspNetCore.OpenIddict.Server",
            ".AspNetCore.Correlation"
        };
        foreach (var name in cookieNames)
        {
            Response.Cookies.Delete(name);
            Response.Cookies.Delete(name, new CookieOptions
            {
                Domain = ".azurewebsites.net",
                Path   = "/",
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }

        return LocalRedirect(ReturnUrl ?? "/");
    }
}
