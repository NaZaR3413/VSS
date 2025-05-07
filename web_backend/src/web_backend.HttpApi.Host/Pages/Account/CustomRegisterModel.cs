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


namespace web_backend.HttpApi.Host.Pages.Account;
public class CustomRegisterModel : Volo.Abp.Account.Web.Pages.Account.RegisterModel
{
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<Volo.Abp.Identity.IdentityUser, Guid> _identityUserRepository;

    public CustomRegisterModel(
        IdentityUserManager userManager,
        IAccountAppService accountAppService,
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IdentityDynamicClaimsPrincipalContributorCache claimsPrincipalContributionCache,
        IRepository<Volo.Abp.Identity.IdentityUser, Guid> identityUserRepository
    ) : base(accountAppService, schemeProvider, accountOptions, claimsPrincipalContributionCache)
    {
        _identityUserRepository = identityUserRepository;
        _userManager = userManager;
        Input = new CustomRegisterInput(); // Use CustomRegisterInput instead of PostInput
    }



    [BindProperty]
    public new CustomRegisterInput Input { get; set; } // Override Input with CustomRegisterInput


    public override async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page(); // Return to the same page if validation fails
        }
        try
        {
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
            return LocalRedirect(ReturnUrl ?? "/");

        }
        catch (UserFriendlyException ex)
        {
            ViewData["ErrorMessage"] = ex.Message; // Set the error message
            return Page(); // Return the page with the error message
        }
        catch (BusinessException e)
        {
            Alerts.Danger(GetLocalizeExceptionMessage(e));
            return Page();
        }

    }

    protected override async Task RegisterLocalUserAsync()
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
        //ValidateModel();
        if (!ModelState.IsValid)
        {
            return;
        }

        var userDto = await AccountAppService.RegisterAsync(
            new web_backend.Pages.Account.CustomRegisterDto
            {
                AppName = "MVC",
                EmailAddress = Input.EmailAddress,
                Password = Input.Password,
                UserName = Input.UserName,
                Name = Input.Name,
                PhoneNumber = normalizedPhone,
                ConfirmPassword = Input.ConfirmPassword,
                AcceptTerms = Input.AcceptTerms
            }
        );

        var user = await UserManager.GetByIdAsync(userDto.Id);
        if (!string.IsNullOrEmpty(Input.Name))
        {
            user.SetProperty("Name", Input.Name);
        }
        if (!string.IsNullOrEmpty(normalizedPhone))
        {
            user.SetProperty("PhoneNumber", normalizedPhone);
        }
        await SignInManager.SignInAsync(user, isPersistent: true);

        // Clear the dynamic claims cache.
        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(user.Id, user.TenantId);
    }

    public class CustomRegisterInput : Volo.Abp.Account.Web.Pages.Account.RegisterModel.PostInput
    {
        [Required(ErrorMessage = "Name is required.")]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxNameLength))]
        public string Name { get; set; }

        
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPhoneNumberLength))]
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
