using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.Account.Emailing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using web_backend.Pages.Account;

namespace web_backend.Account
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAccountAppService), typeof(AccountAppService))]
    public class CustomAccountAppService : AccountAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly ILogger<CustomAccountAppService> _logger;

        public CustomAccountAppService(
            IdentityUserManager userManager,
            IIdentityRoleRepository roleRepository,
            IAccountEmailer accountEmailer,
            IdentitySecurityLogManager identitySecurityLogManager,
            IOptions<IdentityOptions> identityOptions,
            ILogger<CustomAccountAppService> logger) 
            : base(userManager, roleRepository, accountEmailer, identitySecurityLogManager, identityOptions)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public override async Task<IdentityUserDto> RegisterAsync(RegisterDto input)
        {
            // If we have a CustomRegisterDto, handle the extra properties
            if (input is CustomRegisterDto customInput)
            {
                _logger.LogInformation("Processing custom registration with name: {Name}", customInput.Name);
                
                // Perform the base registration logic first
                var result = await base.RegisterAsync(input);
                
                // Now handle the extra fields that aren't part of the base RegisterDto
                if (result != null)
                {
                    try
                    {
                        var user = await _userManager.GetByIdAsync(result.Id);
                        
                        // Set the extra properties
                        if (!string.IsNullOrEmpty(customInput.Name))
                        {
                            user.Name = customInput.Name;
                        }
                        
                        if (!string.IsNullOrEmpty(customInput.PhoneNumber))
                        {
                            user.SetPhoneNumber(customInput.PhoneNumber, true);
                        }
                        
                        // Save the changes
                        await _userManager.UpdateAsync(user);
                        
                        _logger.LogInformation("Successfully updated user with custom fields, ID: {UserId}", user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating user with custom fields");
                        throw;
                    }
                }
                
                return result;
            }
            
            // Otherwise, use the base implementation
            return await base.RegisterAsync(input);
        }
    }
}