namespace EmailManager.Client.Model
{
    public class Email
    {
        public string Subject { get; set; }
        public DateTimeOffset? ReceivedDateTime { get; set; }
        public string Sender { get; set; }
    }
}
