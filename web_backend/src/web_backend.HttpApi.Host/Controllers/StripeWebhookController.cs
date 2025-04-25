using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System;
using System.IO;
using System.Threading.Tasks;
using web_backend.Stripe;
using static IdentityModel.OidcConstants;

namespace web_backend.Controllers
{
    [Route("api/stripe/webhook")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly StripeSubscriptionAppService _stripeSubscriptionAppService;
        private readonly StripeCancellationAppService _stripeCancellationAppService;
        private readonly ILogger<StripeCancellationAppService> _logger;

        public StripeWebhookController(
            IConfiguration config,
            StripeSubscriptionAppService stripeAppService,
            StripeCancellationAppService stripeCancellationAppService,
            ILogger<StripeCancellationAppService> logger
            )
        {
            _config = config;
            _stripeSubscriptionAppService = stripeAppService;
            _stripeCancellationAppService = stripeCancellationAppService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var secret = _config["Stripe:WebhookSecret"];
            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    secret
                );
            }
            catch (StripeException ex)
            {
                return BadRequest(); // Signature verification failed
            }
            // user bought a subscription
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                await _stripeSubscriptionAppService.HandleSubscriptionAsync(session.Id, session.SubscriptionId);
            }
            // user cancelling a subscription
            if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var subscription = stripeEvent.Data.Object as Subscription;
                if (subscription == null) return NoContent();

                // pull userId out of metadata
                if (!subscription.Metadata.TryGetValue("userId", out var userId)
                    || string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Subscription {SubId} deleted but no userId metadata", subscription.Id);
                    return NoContent();
                }

                try
                {
                    await _stripeCancellationAppService.HandleSubscriptionCanceledAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling subscription cancellation for user {UserId}", userId);
                }
            }


            return NoContent(); // Stripe expects a 2xx
        }
    }
}
