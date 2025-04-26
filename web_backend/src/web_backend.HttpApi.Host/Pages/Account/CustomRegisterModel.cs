using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Account.Web;
using Volo.Abp.Data;
using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Validation;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using System.Text.RegularExpressions;
using System.Linq; // Add this for the Select() extension method

namespace web_backend.HttpApi.Host.Pages.Account;
public class CustomRegisterModel : Volo.Abp.Account.Web.Pages.Account.RegisterModel
{
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<Volo.Abp.Identity.IdentityUser, Guid> _identityUserRepository;
    private readonly ILogger<CustomRegisterModel> _logger;

    public CustomRegisterModel(
        IdentityUserManager userManager,
        IAccountAppService accountAppService,
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IdentityDynamicClaimsPrincipalContributorCache claimsPrincipalContributionCache,
        IRepository<Volo.Abp.Identity.IdentityUser, Guid> identityUserRepository,
        ILogger<CustomRegisterModel> logger
    ) : base(accountAppService, schemeProvider, accountOptions, claimsPrincipalContributionCache)
    {
        _identityUserRepository = identityUserRepository;
        _userManager = userManager;
        _logger = logger;
        Input = new CustomRegisterInput();
    }

    [BindProperty]
    public new CustomRegisterInput Input { get; set; }

    public override async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Return to the same page if validation fails
            }

            _logger.LogInformation("Registration attempt for user: {Email}", Input.EmailAddress);
            
            ExternalProviders = await GetExternalProviders();

            if (!await CheckSelfRegistrationAsync())
            {
                throw new UserFriendlyException(L["SelfRegistrationDisabledMessage"]);
            }

            if (IsExternalLogin)
            {
                var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo == null)
                {
                    Logger.LogWarning("External login info is not available");
                    return RedirectToPage("./Login");
                }
                if (Input.UserName.IsNullOrWhiteSpace())
                {
                    Input.UserName = await UserManager.GetUserNameFromEmailAsync(Input.EmailAddress);
                }
                await RegisterExternalUserAsync(externalLoginInfo, Input.UserName, Input.EmailAddress);
            }
            else
            {
                await RegisterLocalUserAsync();
            }
            
            _logger.LogInformation("Registration successful for user: {Email}", Input.EmailAddress);
            
            // Parse and validate the return URL to ensure it's allowed
            var returnUrl = ReturnUrl;
            if (!string.IsNullOrEmpty(returnUrl))
            {
                _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", returnUrl);
                
                if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                {
                    // Check if it's in the allowed redirects list
                    var redirectAllowedUrls = await SettingProvider.GetOrNullAsync("App.RedirectAllowedUrls");
                    var allowedUrls = redirectAllowedUrls?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    
                    if (allowedUrls.Any(url => uri.AbsoluteUri.StartsWith(url, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If it's allowed, use plain Redirect
                        return Redirect(returnUrl);
                    }
                    
                    _logger.LogWarning("Attempted redirect to non-allowed URL: {ReturnUrl}", returnUrl);
                    // If not allowed, redirect to home
                    return LocalRedirect("~/");
                }
                
                return LocalRedirect(returnUrl);
            }
            
            return LocalRedirect("~/");
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User-friendly exception during registration: {Message}", ex.Message);
            ViewData["ErrorMessage"] = ex.Message;
            return Page();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during registration: {Message}", e.Message);
            ViewData["ErrorMessage"] = "An error occurred during registration. Please try again or contact support.";
            return Page();
        }
    }

    protected override async Task RegisterLocalUserAsync()
    {
        try
        {
            string? rawPhone = Input.PhoneNumber;
            string? normalizedPhone = null;

            if (!string.IsNullOrWhiteSpace(rawPhone))
            {
                // Normalize the phone number - remove all non-digit characters
                normalizedPhone = Regex.Replace(rawPhone, @"\D", "");

                if (string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    throw new UserFriendlyException("Invalid phone number format.");
                }

                // Check if already exists
                var existingUser = await _identityUserRepository
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

                if (existingUser != null)
                {
                    throw new UserFriendlyException("The phone number is already registered.");
                }
            }
            
            if (!Input.AcceptTerms)
            {
                throw new UserFriendlyException("You must accept the terms and conditions to register.");
            }
            
            if (!ModelState.IsValid)
            {
                throw new UserFriendlyException("Please correct the errors and try again.");
            }

            _logger.LogInformation("Creating user with standard registration process");
            
            // Create a standard RegisterDto to avoid serialization issues
            var registerDto = new Volo.Abp.Account.RegisterDto
            {
                AppName = "MVC",
                EmailAddress = Input.EmailAddress,
                Password = Input.Password,
                UserName = Input.UserName
            };
            
            try
            {
                _logger.LogInformation("Calling AccountAppService.RegisterAsync");
                var userDto = await AccountAppService.RegisterAsync(registerDto);
                _logger.LogInformation("User registered successfully with ID: {UserId}", userDto.Id);
                
                // Now update the user with additional properties
                var user = await UserManager.GetByIdAsync(userDto.Id);
                
                if (!string.IsNullOrEmpty(Input.Name))
                {
                    user.Name = Input.Name;
                    _logger.LogInformation("Setting user name to: {Name}", Input.Name);
                }
                
                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    user.SetPhoneNumber(normalizedPhone, true);
                    _logger.LogInformation("Setting phone number to: {Phone}", normalizedPhone);
                }
                
                // Save the changes
                var updateResult = await UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("User update failed: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    throw new UserFriendlyException("Registration partially succeeded but user details could not be updated.");
                }
                
                _logger.LogInformation("User updated with custom fields");
                
                await SignInManager.SignInAsync(user, isPersistent: true);
                _logger.LogInformation("User signed in successfully");

                // Clear the dynamic claims cache.
                await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(user.Id, user.TenantId);
            }
            catch (AbpValidationException validationEx)
            {
                _logger.LogWarning(validationEx, "Validation error during registration");
                foreach (var error in validationEx.ValidationErrors)
                {
                    ModelState.AddModelError(string.Empty, error.ErrorMessage);
                }
                throw new UserFriendlyException("Registration validation failed: " + string.Join("; ", validationEx.ValidationErrors.Select(e => e.ErrorMessage)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterLocalUserAsync: {Message}", ex.Message);
            throw;
        }
    }

    public class CustomRegisterInput : Volo.Abp.Account.Web.Pages.Account.RegisterModel.PostInput
    {
        [Required(ErrorMessage = "Name is required.")]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxNameLength))]
        public string Name { get; set; }
        
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPhoneNumberLength))]
        [Phone]
        [RegularExpression(@"^[\d\-\+\(\) ]+$", ErrorMessage = "Enter a valid phone number.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please confirm password.")]
        [DataType(DataType.Password)]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "You must accept the terms and conditions to register.")]
        public bool AcceptTerms { get; set; }
    }
}
