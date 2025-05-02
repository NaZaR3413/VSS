using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Uow;
using Volo.Abp.Domain.Repositories;
using System;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using System.Linq;
using Microsoft.Extensions.Options;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;
using static Volo.Abp.Identity.IdentityPermissions;
using Volo.Abp.Emailing;

namespace web_backend.HttpApi.Host.Pages.Account
{
    [Authorize]
    public class CustomProfileModel : PageModel
    {
        private readonly ICurrentUser _currentUser;
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<Volo.Abp.Identity.IdentityUser, Guid> _userRepository;
        private readonly IEmailSender _emailSender;
        public CustomProfileModel(
            ICurrentUser currentUser,
            IdentityUserManager userManager,
            IRepository<Volo.Abp.Identity.IdentityUser, Guid> userRepository,
            IEmailSender emailSender)
        {
            _currentUser = currentUser;
            _userManager = userManager;
            _userRepository = userRepository;
            _emailSender = emailSender;
        }

        public string NewName { get; set; }
        public string NewPhoneNumber { get; set; }
        public string UserRole { get; set; }

        [BindProperty]
        public string CurrentPassword { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        public async Task OnGetAsync()
        {
            if (_currentUser.IsAuthenticated)
            {
                var userId = _currentUser.GetId();
                var user = await _userManager.GetByIdAsync(userId);
                var roles = await _userManager.GetRolesAsync(user);
                UserRole = roles.FirstOrDefault() ?? "user";
            }
        }

        [UnitOfWork]
        public async Task<IActionResult> OnPostDeleteAccountAsync()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _currentUser.GetId();
            var user = await _userRepository.GetAsync(userId);

            if (user == null)
            {
                return NotFound();
            }
            await _userRepository.DeleteDirectAsync(u => u.Id == userId); // delete user
            return Redirect("~/Account/Logout");
        }

        [UnitOfWork]
        public async Task<IActionResult> OnPostUpdateNameAsync()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _currentUser.GetId();
            var user = await _userRepository.GetAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var newName = Request.Form["NewName"].ToString();

            if (!string.IsNullOrWhiteSpace(newName))
            {
                user.Name = newName.Trim();
                user.SetProperty("Name", newName);
                TempData["NameChangeSuccess"] = "Name changed successfully.";
            }

            await _userRepository.UpdateAsync(user, autoSave: true);
            return RedirectToPage();
        }

        [UnitOfWork]
        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _currentUser.GetId();
            var user = await _userManager.GetByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            UserRole = roles.FirstOrDefault() ?? "user";
            NewName = user.Name;
            NewPhoneNumber = user.PhoneNumber;

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return Page();
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);

            if (result.Succeeded)
            {
                TempData["PasswordChangeSuccess"] = "Password changed successfully.";
                return RedirectToPage(); // Refresh the page
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            // Restore user info
            UserRole = roles.FirstOrDefault() ?? "user";
            NewName = user.Name;
            NewPhoneNumber = user.PhoneNumber;

            return Page();
        }

        [UnitOfWork]
        public async Task<IActionResult> OnPostSendEmailVerificationAsync()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _currentUser.GetId();
            var user = await _userManager.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (user.EmailConfirmed)
            {
                TempData["EmailVerificationInfo"] = "Email is already verified.";
                return RedirectToPage();
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            var subject = "Email Confirmation";
            var body = $"Hi {user.UserName},<br/><br/>Please confirm your email by <a href='{confirmationUrl}'>clicking here</a>.<br/><br/>Varsity Sports Show";

            await _emailSender.SendAsync(
                to: user.Email,
                subject: subject,
                body: body,
                isBodyHtml: true
            );

            TempData["EmailVerificationSent"] = "Verification email has been sent.";
            return RedirectToPage();
        }
    }
}