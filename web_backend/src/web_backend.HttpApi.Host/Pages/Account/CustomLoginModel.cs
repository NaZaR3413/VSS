using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Account.Settings;
using Volo.Abp.Account.Web;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace web_backend.HttpApi.Host.Pages.Account
{
    [ExposeServices(typeof(LoginModel), IncludeSelf = true)]
    [Dependency(ReplaceServices = true)]
    public class CustomLoginModel : LoginModel
    {
        private readonly ILogger<CustomLoginModel> _logger;
        private readonly string _frontEndRoot;

        public CustomLoginModel(
            IAuthenticationSchemeProvider schemeProvider,
            IOptions<AbpAccountOptions> accountOptions,
            IOptions<IdentityOptions> identityOptions,
            IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)                    // ⬅ inject configuration
            : base(schemeProvider, accountOptions, identityOptions, identityDynamicClaimsPrincipalContributorCache)
        {
            _logger        = loggerFactory.CreateLogger<CustomLoginModel>();
            _frontEndRoot  = configuration["App:ClientUrl"]        // e.g. https://salmon-glacier-…azurestaticapps.net
                          ?? "https://salmon-glacier-08dca301e.6.azurestaticapps.net";
        }

        public override async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                _logger.LogInformation("Login attempt started for user: {User}", LoginInput?.UserNameOrEmailAddress);

                await CheckLocalLoginAsync();
                ValidateModel();

                ExternalProviders = await GetExternalProviders();
                EnableLocalLogin  = await SettingProvider.IsTrueAsync(AccountSettingNames.EnableLocalLogin);
                await ReplaceEmailToUsernameOfInputIfNeeds();
                await IdentityOptions.SetAsync();

                var result = await SignInManager.PasswordSignInAsync(
                    LoginInput.UserNameOrEmailAddress,
                    LoginInput.Password,
                    LoginInput.RememberMe,
                    true);

                await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext
                {
                    Identity  = IdentitySecurityLogIdentityConsts.Identity,
                    Action    = result.ToIdentitySecurityLogAction(),
                    UserName  = LoginInput.UserNameOrEmailAddress
                });

                if (result.RequiresTwoFactor)   return await TwoFactorLoginResultAsync();
                if (result.IsLockedOut)        { Alerts.Warning(L["UserLockedOutMessage"]);   return Page(); }
                if (result.IsNotAllowed)       { Alerts.Warning(L["LoginIsNotAllowed"]);      return Page(); }
                if (!result.Succeeded)         { Alerts.Danger (L["InvalidUserNameOrPassword"]); return Page(); }

                // ---------------- Final redirect logic ----------------
                // 1️⃣ explicit ReturnUrl present?
                if (!string.IsNullOrWhiteSpace(ReturnUrl))
                {
                    // absolute URL?
                    if (Uri.TryCreate(ReturnUrl, UriKind.Absolute, out var uri))
                    {
                        var allowed = new[]
                        {
                            "salmon-glacier-08dca301e.6.azurestaticapps.net",
                            "vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net"
                        };

                        if (allowed.Any(d => uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase)))
                            return Redirect(ReturnUrl);

                        _logger.LogWarning("Blocked redirect to non-allowed host: {Host}", uri.Host);
                        return Redirect(_frontEndRoot + "/landing");
                    }

                    // relative url → stay on back-end origin
                    return LocalRedirect(ReturnUrl);
                }

                // 2️⃣ no ReturnUrl → send to front-end landing page
                return Redirect(_frontEndRoot + "/landing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                Alerts.Danger("An error occurred during login. Please try again later.");
                return Page();
            }
        }

        // keep Input DTO as originally generated by Abp (no changes needed)
        public class LoginInputModel
        {
            [Required]
            public string UserNameOrEmailAddress { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }
    }
}
