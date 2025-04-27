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
        private readonly ILogger<CustomLoginModel> _logger;

        public CustomLoginModel(
            IAuthenticationSchemeProvider schemeProvider,
            IOptions<AbpAccountOptions> accountOptions,
            IOptions<IdentityOptions> identityOptions,
            IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
            ILogger<CustomLoginModel> logger
        ) : base(schemeProvider, accountOptions, identityOptions, identityDynamicClaimsPrincipalContributorCache)
        {
            _logger = logger;
        }

        public override async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Handle GET requests for the login page
                return await base.OnGetAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync: {Message}", ex.Message);
                Alerts.Danger("An error occurred while loading the login page. Please try again.");
                return Page();
            }
        }

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
                    try
                    {
                        // Frontend application URL
                        string frontendUrl = "https://salmon-glacier-08dca301e.6.azurestaticapps.net";
                        
                        // Local development frontend URL (for testing)
                        string localFrontendUrl = "https://localhost:44307";
                        
                        // Default URL to redirect to after successful login
                        string defaultRedirectUrl = frontendUrl;
                        
                        #if DEBUG
                        // Use local frontend URL for development
                        defaultRedirectUrl = localFrontendUrl;
                        #endif
                        
                        _logger.LogInformation("Login successful for user: {UserName}", LoginInput.UserNameOrEmailAddress);
                        
                        // Check if ReturnUrl is provided
                        if (!string.IsNullOrEmpty(ReturnUrl))
                        {
                            _logger.LogInformation("Processing return URL: {ReturnUrl}", ReturnUrl);
                            
                            // Check if ReturnUrl is a valid absolute URL
                            if (Uri.TryCreate(ReturnUrl, UriKind.Absolute, out var uri))
                            {
                                // Get allowed redirect URLs from settings
                                var redirectAllowedUrls = await SettingProvider.GetOrNullAsync("App.RedirectAllowedUrls");
                                var allowedUrls = !string.IsNullOrEmpty(redirectAllowedUrls)
                                    ? redirectAllowedUrls.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    : new[] { frontendUrl, localFrontendUrl };

                                // Check if the URL is in the allowed list
                                if (allowedUrls.Any(url => uri.AbsoluteUri.StartsWith(url, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _logger.LogInformation("Redirecting to allowed external URL: {Url}", uri.AbsoluteUri);
                                    return Redirect(ReturnUrl);
                                }
                                else
                                {
                                    _logger.LogWarning("External URL not in allowed list: {Url}", uri.AbsoluteUri);
                                }
                            }
                        }
                        
                        // If ReturnUrl is not valid or not in allowed list, redirect to frontend home
                        _logger.LogInformation("Redirecting to frontend application: {Url}", defaultRedirectUrl);
                        return Redirect(defaultRedirectUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing return URL: {Message}", ex.Message);
                        return Redirect("https://salmon-glacier-08dca301e.6.azurestaticapps.net");
                    }
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login process: {Message}", ex.Message);
                Alerts.Danger("An error occurred while processing your login. Please try again.");
                return Page();
            }
        }
    }
}
