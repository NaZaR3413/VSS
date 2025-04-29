using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;

namespace web_backend.HttpApi.Host.Pages.Account
{
    [ExposeServices(typeof(LogoutModel), IncludeSelf = true)]
    [Dependency(ReplaceServices = true)]
    public class CustomLogoutModel : LogoutModel
    {
        private readonly SignInManager<Volo.Abp.Identity.IdentityUser> _signInManager;
        private readonly ILogger<CustomLogoutModel> _logger;
        private readonly string _frontEndRoot;

        public bool ShowLogoutPrompt { get; private set; } = true;
        public string ReturnUrl { get; private set; }

        public CustomLogoutModel(
            SignInManager<Volo.Abp.Identity.IdentityUser> signInManager,
            ILogger<CustomLogoutModel> logger,
            IConfiguration configuration)
            : base()
        {
            _signInManager = signInManager;
            _logger = logger;
            _frontEndRoot = configuration["App:ClientUrl"] ??
                          "https://salmon-glacier-08dca301e.6.azurestaticapps.net";
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // Check if external logout is supported
            if ((await _signInManager.GetExternalAuthenticationSchemesAsync()).Any())
            {
                // If external login is supported, show logout prompt
                ShowLogoutPrompt = true;
                return Page();
            }

            // No need to show the prompt if there's no external provider
            ShowLogoutPrompt = false;
            return await OnPostAsync("Logout", returnUrl);
        }

        public async Task<IActionResult> OnPostAsync(string action = null, string returnUrl = null)
        {
            if (action != "Logout")
            {
                // User chose not to logout
                return RedirectToPage("/Index");
            }

            _logger.LogInformation("User logged out");

            // Clear the existing external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await _signInManager.SignOutAsync();

            // Verify returnUrl is allowed
            if (!string.IsNullOrEmpty(returnUrl))
            {
                // Make sure it's a valid absolute URL
                if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                {
                    var allowed = new[]
                    {
                        "salmon-glacier-08dca301e.6.azurestaticapps.net",
                        "vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net"
                    };

                    if (allowed.Any(d => uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If it's allowed, redirect to it
                        return Redirect(returnUrl);
                    }

                    // Otherwise redirect to front-end landing page
                    _logger.LogWarning("Blocked redirect to non-allowed host: {Host}", uri.Host);
                    return Redirect($"{_frontEndRoot}/landing");
                }
            }

            // Default to front-end landing
            return Redirect($"{_frontEndRoot}/landing");
        }
    }
}
