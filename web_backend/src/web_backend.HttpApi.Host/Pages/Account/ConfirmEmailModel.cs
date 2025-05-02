using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace web_backend.HttpApi.Host.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<Volo.Abp.Identity.IdentityUser> _userManager;

        public ConfirmEmailModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid userId, string token)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            {
                StatusMessage = "Invalid confirmation request.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                StatusMessage = "User not found.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            StatusMessage = result.Succeeded ? "Your email has been confirmed!" : "Error confirming email.";
            return Page();
        }
    }
}