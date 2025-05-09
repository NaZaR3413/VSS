using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Stripe
{
    public class StripeOptions
    {
        public string SecretKey { get; set; }
        public string WebhookSecret { get; set; }
    }
}
