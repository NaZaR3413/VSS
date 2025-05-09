using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;
using System.Linq;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;

namespace web_backend.Stripe
{
    public class StripeCancellationAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<StripeCancellationAppService> _logger;
        private readonly IIdentityUserRepository _userRepository;

        public StripeCancellationAppService(
            IdentityUserManager userManager, 
            ICurrentUser currentUser,
            ILogger<StripeCancellationAppService> logger,
            IIdentityUserRepository userRepository
            )
        {
            _userManager = userManager;
            _currentUser = currentUser;
            _logger = logger;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Called by your Blazor UI to cancel the current user's subscription.
        /// Schedules cancellation at the end of the billing period.
        /// </summary>
        public async Task CancelSubscriptionAsync()
        {
            var userId = _currentUser.Id?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                throw new UserFriendlyException("You must be logged in to cancel.");
            }

            var user = await _userManager.GetByIdAsync(Guid.Parse(userId));
            if (!await _userManager.IsInRoleAsync(user, "subscriber"))
            {
                throw new UserFriendlyException("You do not have an active subscription.");
            }

            var stripeCustomerId = user.ExtraProperties.GetOrDefault("StripeCustomerId")?.ToString();
            var stripeSubscriptionId = user.ExtraProperties.GetOrDefault("StripeSubscriptionId")?.ToString();
            _logger.LogInformation("Debug: StripeCustomerId: {CustomerId}, StripeSubscriptionId: {SubscriptionId}", stripeCustomerId, stripeSubscriptionId);

            if (string.IsNullOrEmpty(stripeCustomerId) || string.IsNullOrEmpty(stripeSubscriptionId))
            {
                throw new UserFriendlyException("Subscription information not found.");
            }

            // Schedule cancellation at end of period
            var subscriptionService = new SubscriptionService();
            var updateOptions = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };
            await subscriptionService.UpdateAsync(stripeSubscriptionId, updateOptions);

        }

        /// <summary>
        /// Called from your webhook handler when Stripe fires customer.subscription.deleted.
        /// Removes the "subscriber" role (and re-adds "user" if you wish).
        /// </summary>
        public async Task HandleSubscriptionCanceledAsync(string userId)
        {
            // 1) Load your ABP user directly by their GUID
            if (!Guid.TryParse(userId, out var guid))
            {
                throw new UserFriendlyException("Invalid userId in webhook payload.");
            }

            var user = await _userManager.GetByIdAsync(guid);
            if (user == null)
            {
                _logger.LogWarning("No ABP user found for ID {UserId}", userId);
                return;
            }

            // 2) Remove subscriber role
            if (await _userManager.IsInRoleAsync(user, "subscriber"))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, "subscriber");
                if (removeResult.Succeeded)
                {
                    _logger.LogInformation("Removed 'subscriber' role from {UserName}", user.UserName);
                }
                else
                {
                    _logger.LogWarning(
                       "Failed to remove subscriber role from {UserName}: {Errors}",
                       user.UserName,
                       string.Join(", ", removeResult.Errors.Select(e => e.Description))
                    );
                }
            }

            // 3) Re-add the default 'user' role if needed
            if (!await _userManager.IsInRoleAsync(user, "user"))
            {
                var addResult = await _userManager.AddToRoleAsync(user, "user");
                if (addResult.Succeeded)
                {
                    _logger.LogInformation("Re-added 'user' role to {UserName}", user.UserName);
                }
                else
                {
                    _logger.LogWarning(
                       "Failed to add 'user' role to {UserName}: {Errors}",
                       user.UserName,
                       string.Join(", ", addResult.Errors.Select(e => e.Description))
                    );
                }
            }

            // remove the customerid and subscriptionid from extra properties. are not needed now that subscription has been cancelled
            var removedAny = false;
            if (user.ExtraProperties.Remove("StripeCustomerId"))
            {
                _logger.LogInformation("Cleared StripeCustomerId for {UserName}", user.UserName);
                removedAny = true;
            }
            if (user.ExtraProperties.Remove("StripeSubscriptionId"))
            {
                _logger.LogInformation("Cleared StripeSubscriptionId for {UserName}", user.UserName);
                removedAny = true;
            }

            // Persist the property removals if needed
            if (removedAny)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning(
                      "Failed to persist ExtraProperties cleanup for {UserName}: {Errors}",
                      user.UserName,
                      string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );
                }
            }
        }

    }
}
