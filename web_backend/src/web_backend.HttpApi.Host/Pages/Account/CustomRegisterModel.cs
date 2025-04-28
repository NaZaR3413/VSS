using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace web_backend.HttpApi.Host.Pages.Account;

public class CustomRegisterModel : RegisterModel
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
        ILogger<CustomRegisterModel> logger)
        : base(accountAppService, schemeProvider, accountOptions, claimsPrincipalContributionCache)
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
        if (!ModelState.IsValid)
        {
            return Page();
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
                var extInfo = await SignInManager.GetExternalLoginInfoAsync();
                if (extInfo == null)
                {
                    _logger.LogWarning("External login info unavailable");
                    return RedirectToPage("./Login");
                }

                if (Input.UserName.IsNullOrWhiteSpace())
                {
                    Input.UserName = await UserManager.GetUserNameFromEmailAsync(Input.EmailAddress);
                }

                await RegisterExternalUserAsync(extInfo, Input.UserName, Input.EmailAddress);
            }
            else
            {
                await RegisterLocalUserAsync();
            }

            /*                                             
             * IMPORTANT: do NOT sign the user in automatically.
             * The user should log in manually after confirming registration.
             */

            return LocalRedirect(Url.Page("./Login", new { returnUrl = ReturnUrl, returnUrlHash = ReturnUrlHash }));
        }
        catch (UserFriendlyException ex)
        {
            ViewData["ErrorMessage"] = ex.Message;
            return Page();
        }
        catch (BusinessException ex)
        {
            Alerts.Danger(GetLocalizeExceptionMessage(ex));
            return Page();
        }
    }

    protected override async Task RegisterLocalUserAsync()
    {
        // 1. Normalise/validate phone
        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
        {
            normalizedPhone = Regex.Replace(Input.PhoneNumber, @"\\D", "");
            if (string.IsNullOrEmpty(normalizedPhone))
                throw new UserFriendlyException("Invalid phone number format.");

            var duplicate =
                await _identityUserRepository.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (duplicate != null)
                throw new UserFriendlyException("The phone number is already registered.");
        }

        // 2. Terms & validation
        if (!Input.AcceptTerms)
            throw new UserFriendlyException("You must accept the terms and conditions to register.");

        if (!ModelState.IsValid)
            return;

        // 3. Create user through the application service
        var userDto = await AccountAppService.RegisterAsync(new web_backend.Pages.Account.CustomRegisterDto
        {
            AppName        = "MVC",
            EmailAddress   = Input.EmailAddress,
            Password       = Input.Password,
            UserName       = Input.UserName,
            Name           = Input.Name,
            PhoneNumber    = normalizedPhone,
            ConfirmPassword= Input.ConfirmPassword,
            AcceptTerms    = Input.AcceptTerms
        });

        // 4. Store Name / PhoneNumber as extra properties
        var user = await _userManager.GetByIdAsync(userDto.Id);
        if (!string.IsNullOrEmpty(Input.Name))
            user.SetProperty("Name", Input.Name);

        if (!string.IsNullOrEmpty(normalizedPhone))
            user.SetProperty("PhoneNumber", normalizedPhone);

        // persist extra properties
        await _userManager.UpdateAsync(user);

        // clear dynamic-claim cache
        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(user.Id, user.TenantId);
    }

    public class CustomRegisterInput : RegisterModel.PostInput
    {
        [Required]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxNameLength))]
        public string Name { get; set; }

        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPhoneNumberLength))]
        [RegularExpression(@"^[\\d\\-\\+\\(\\) ]+$", ErrorMessage = "Enter a valid phone number.")]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "You must accept the terms and conditions to register.")]
        public bool AcceptTerms { get; set; }
    }
}
