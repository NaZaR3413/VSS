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
using OpenIddict.Abstractions;
using System.Security.Principal;
using OpenIddict.Server.AspNetCore;

namespace web_backend.HttpApi.Host.Pages.Account
{
    [ExposeServices(typeof(LoginModel), IncludeSelf = true)]
    [Dependency(ReplaceServices = true)]
    public class CustomLoginModel : LoginModel
    {
        private readonly ILogger<CustomLoginModel> _logger;
        private readonly IOpenIddictApplicationManager _applicationManager;

        public CustomLoginModel(
            IAuthenticationSchemeProvider schemeProvider,
            IOptions<AbpAccountOptions> accountOptions,
            IOptions<IdentityOptions> identityOptions,
            IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
            ILoggerFactory loggerFactory,
            IOpenIddictApplicationManager applicationManager
        ) : base(schemeProvider, accountOptions, identityOptions, identityDynamicClaimsPrincipalContributorCache)
        {
            _logger = loggerFactory.CreateLogger<CustomLoginModel>();
            _applicationManager = applicationManager;
        }

        public override async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                _logger.LogInformation("Login attempt started for user: {UserName}", LoginInput?.UserNameOrEmailAddress);
                
                await CheckLocalLoginAsync();
                _logger.LogInformation("CheckLocalLoginAsync completed");
                
                ValidateModel();
                _logger.LogInformation("Model validation completed");

                // External login providers
                ExternalProviders = await GetExternalProviders();
                EnableLocalLogin = await SettingProvider.IsTrueAsync(AccountSettingNames.EnableLocalLogin);

                await ReplaceEmailToUsernameOfInputIfNeeds();
                _logger.LogInformation("Email to username replacement completed");

                await IdentityOptions.SetAsync();
                _logger.LogInformation("IdentityOptions set completed");

                _logger.LogInformation("Attempting password sign in for user: {UserName}", LoginInput.UserNameOrEmailAddress);
                var result = await SignInManager.PasswordSignInAsync(
                    LoginInput.UserNameOrEmailAddress,
                    LoginInput.Password,
                    LoginInput.RememberMe,
                    true
                );
                _logger.LogInformation("Password sign-in result: {Result}", result.ToString());

                await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext()
                {
                    Identity = IdentitySecurityLogIdentityConsts.Identity,
                    Action = result.ToIdentitySecurityLogAction(),
                    UserName = LoginInput.UserNameOrEmailAddress
                });
                _logger.LogInformation("Security log saved");

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("Two-factor authentication required");
                    return await TwoFactorLoginResultAsync();
                }

                if (result.IsLockedOut)
                {
                    _logger.LogInformation("User account is locked out");
                    Alerts.Warning(L["UserLockedOutMessage"]);
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    _logger.LogInformation("Login is not allowed");
                    Alerts.Warning(L["LoginIsNotAllowed"]);
                    return Page();
                }

                if (!result.Succeeded)
                {
                    _logger.LogInformation("Invalid username or password");
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                var user = await UserManager.FindByNameAsync(LoginInput.UserNameOrEmailAddress) ??
                           await UserManager.FindByEmailAsync(LoginInput.UserNameOrEmailAddress);

                if (user == null)
                {
                    _logger.LogInformation("User not found");
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                // We don't need to directly sign in with OpenIddict here.
                // The standard Identity sign-in is sufficient, and OpenIddict will use that authentication
                // when the user is redirected to the OIDC endpoints.
                _logger.LogInformation("Login succeeded for user: {UserName}, redirecting to: {ReturnUrl}", 
                    LoginInput.UserNameOrEmailAddress, ReturnUrl ?? "/");
                
                // Check if ReturnUrl is absolute (external) or relative
                if (Uri.TryCreate(ReturnUrl, UriKind.Absolute, out var uri))
                {
                    // It's an absolute URL, use Redirect instead of LocalRedirect
                    _logger.LogInformation("Using Redirect for external URL: {Url}", ReturnUrl);
                    return Redirect(ReturnUrl);
                }
                else
                {
                    // It's a relative URL, use LocalRedirect
                    var localUrl = ReturnUrl ?? "/";
                    _logger.LogInformation("Using LocalRedirect for local URL: {Url}", localUrl);
                    return LocalRedirect(localUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                    _logger.LogError("Inner exception stack trace: {InnerExceptionStackTrace}", ex.InnerException.StackTrace);
                }
                
                Alerts.Danger("An error occurred during login. Please try again later.");
                return Page();
            }
        }
    }
}
