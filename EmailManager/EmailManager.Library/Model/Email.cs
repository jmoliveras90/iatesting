using System;

namespace EmailManager.Library.Model
{
    public class Email
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public DateTimeOffset? ReceivedDateTime { get; set; }
        public string Sender { get; set; }
    }
}
