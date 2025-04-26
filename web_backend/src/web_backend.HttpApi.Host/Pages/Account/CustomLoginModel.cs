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

                var user = await UserManager.FindByNameAsync(LoginInput.UserNameOrEmailAddress) ??
                           await UserManager.FindByEmailAsync(LoginInput.UserNameOrEmailAddress);

                if (user == null)
                {
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                if (result.Succeeded)
                {
                    // Use plain Redirect for external URLs to avoid the "localhost cannot be accessed" issues
                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        // Check if ReturnUrl is a valid URL
                        if (Uri.TryCreate(ReturnUrl, UriKind.Absolute, out var uri))
                        {
                            // Check if it's in the allowed redirects list before using it
                            var redirectAllowedUrls = await SettingProvider.GetOrNullAsync("App.RedirectAllowedUrls");
                            var allowedUrls = redirectAllowedUrls?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                            
                            if (allowedUrls.Any(url => uri.AbsoluteUri.StartsWith(url, StringComparison.OrdinalIgnoreCase)))
                            {
                                // If it's an absolute URL and it's in the allowed list, use plain Redirect
                                return Redirect(ReturnUrl);
                            }
                        }
                    }
                    
                    // For local URLs, use LocalRedirect which validates the URL is local
                    return LocalRedirect(ReturnUrl ?? "~/");
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                Alerts.Danger("An error occurred while processing your login. Please try again.");
                return Page();
            }
        }
    }
}
