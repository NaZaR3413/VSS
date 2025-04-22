using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using Volo.Abp.Data;
using Microsoft.AspNetCore.Identity;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;
using Volo.Abp;
using Volo.Abp.Users;


namespace web_backend.Stripe
{
    public class StripeSubscriptionAppService : ApplicationService
    {
        public async Task<string> CreateCheckoutSessionAsync()
        {
            // verify user is logged in
            var userId = CurrentUser.Id?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UserFriendlyException("You must be logged in to subscribe.");
            }

            // verify the user isn't already a subscriber. return an error if they are
            var userManager = LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
            var user = await userManager.GetByIdAsync(Guid.Parse(userId));

            if (await userManager.IsInRoleAsync(user, "subscriber"))
            {
                throw new UserFriendlyException("You are already subscribed.");
            }

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
            {
                { "userId", userId }
            },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = "price_1RGSSk01dSxu5ypyTJawUqwN",

                    Quantity = 1,
                }
            },
                //SuccessUrl = "https://localhost:44320/payment-success?session_id={CHECKOUT_SESSION_ID}",
                //CancelUrl = "https://localhost:44320/payment-cancel"
                SuccessUrl = "https://localhost:44320",
                CancelUrl = "https://localhost:44320/subscribe"
            };

            var sessionService = LazyServiceProvider.LazyGetRequiredService<SessionService>();
            var session = await sessionService.CreateAsync(options);
            return session.Url;
        }

        public async Task HandleSubscriptionAsync(string sessionId)
        {
            var sessionService = LazyServiceProvider.LazyGetRequiredService<SessionService>();
            var session = await sessionService.GetAsync(sessionId);

            if (!session.Metadata.TryGetValue("userId", out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                Logger.LogWarning("No userId found in session metadata.");
                return;
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                Logger.LogWarning("Invalid userId format.");
                return;
            }

            var userManager = LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                Logger.LogWarning($"User not found for ID {userId}");
                return;
            }

            // ad subscriber role to customer 
            if (!await userManager.IsInRoleAsync(user, "subscriber"))
            {
                var result = await userManager.AddToRoleAsync(user, "subscriber");
                if (result.Succeeded)
                {
                    Logger.LogInformation($"User {user.UserName} added to subscriber role.");
                }
                else
                {
                    Logger.LogWarning("Role assignment failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Remove user role if it's present
            if (await userManager.IsInRoleAsync(user, "user"))
            {
                var result = await userManager.RemoveFromRoleAsync(user, "user");

                if (result.Succeeded)
                {
                    Logger.LogInformation($"Removed 'user' role from user {user.UserName}");
                }
                else
                {
                    Logger.LogWarning($"Failed to remove 'user' role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // save customer's customerId under extra propertes in the user's table, {StripeCustomerID: customerID)
            // This way you can retrieve the same customer instance later to cancel billing
            if (!string.IsNullOrEmpty(session.CustomerId))
            {
                user.SetProperty("StripeCustomerId", session.CustomerId);
                await userManager.UpdateAsync(user);
            }

        }

    }

}
