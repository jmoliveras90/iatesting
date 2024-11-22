namespace EmailManager.Client.Model
{
    public class Email
    {
        public required string Id { get; set; }
        public string Subject { get; set; }
        public DateTimeOffset? ReceivedDateTime { get; set; }
        public string Sender { get; set; }
    }
}
