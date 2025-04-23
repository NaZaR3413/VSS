using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.IO;
using System.Threading.Tasks;
using web_backend.Stripe;

namespace web_backend.Controllers
{
    [Route("api/stripe/webhook")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly StripeSubscriptionAppService _stripeSubscriptionAppService;

        public StripeWebhookController(
            IConfiguration config,
            StripeSubscriptionAppService stripeAppService)
        {
            _config = config;
            _stripeSubscriptionAppService = stripeAppService;
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

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                await _stripeSubscriptionAppService.HandleSubscriptionAsync(session.Id, session.Subscription.Id);
            }
            if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var subscription = stripeEvent.Data.Object as Subscription;
                var customerId = subscription.CustomerId;

                // Call  app service
                //await _stripeAppService.HandleSubscriptionCanceledAsync(customerId);
            }


            return NoContent(); // Stripe expects a 2xx
        }
    }
}
