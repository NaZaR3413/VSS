using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Account.Settings;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using Volo.Abp.Validation;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.DependencyInjection;

namespace web_backend.HttpApi.Host.Pages.Account
{
    [ExposeServices(typeof(LoginModel), IncludeSelf = true)]
    [Dependency(ReplaceServices = true)]
    public class CustomLoginModel : LoginModel
    {
        public CustomLoginModel(
            IAuthenticationSchemeProvider schemeProvider,
            IOptions<AbpAccountOptions> accountOptions,
            IOptions<IdentityOptions> identityOptions,
            IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache
        ) : base(schemeProvider, accountOptions, identityOptions, identityDynamicClaimsPrincipalContributorCache)
        { }

        public override async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                await CheckLocalLoginAsync();
                ValidateModel();

                // External login providers
                ExternalProviders = await GetExternalProviders();
                EnableLocalLogin = await SettingProvider.IsTrueAsync(AccountSettingNames.EnableLocalLogin);

                await ReplaceEmailToUsernameOfInputIfNeeds();

                await IdentityOptions.SetAsync();

                var result = await SignInManager.PasswordSignInAsync(
                    LoginInput.UserNameOrEmailAddress,
                    LoginInput.Password,
                    LoginInput.RememberMe,
                    true
                );

                await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext()
                {
                    Identity = IdentitySecurityLogIdentityConsts.Identity,
                    Action = result.ToIdentitySecurityLogAction(),
                    UserName = LoginInput.UserNameOrEmailAddress
                });

                if (result.RequiresTwoFactor)
                {
                    return await TwoFactorLoginResultAsync();
                }

                if (result.IsLockedOut)
                {
                    Alerts.Warning(L["UserLockedOutMessage"]);
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    Alerts.Warning(L["LoginIsNotAllowed"]);
                    return Page();
                }

                if (!result.Succeeded)
                {
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                // At this point we know the login was successful
                // Check if ReturnUrl is valid and safe
                if (!string.IsNullOrEmpty(ReturnUrl) && 
                    ReturnUrl.StartsWith("https://salmon-glacier-08dca301e.6.azurestaticapps.net"))
                {
                    // Special handling for Azure Static Web App return URL
                    // Make sure the return URL is properly processed
                    return Redirect(ReturnUrl);
                }
                
                // Otherwise use the standard local redirect
                return LocalRedirect(ReturnUrl ?? "/");
            }
            catch (Exception ex)
            {
                // Log the exception
                Logger.LogError(ex, "Error during login: {Message}", ex.Message);
                
                // Show a user-friendly error
                Alerts.Danger("An error occurred during login. Please try again.");
                return Page();
            }
        }
    }
}
