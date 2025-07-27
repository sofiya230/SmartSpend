using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSpend.Services
{

    public class SendGridEmailOptions
    {
        public const string Section = "SendGrid";

        public string Key { get; set; }
        public string Email { get; set; }
        public string Sender { get; set; }
    }
}
