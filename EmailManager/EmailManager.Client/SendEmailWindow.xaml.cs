using EmailManager.Library.Enum;
using EmailManager.Library.Services;
using Google.Apis.Gmail.v1;
using System.Windows;

namespace EmailManager.Client
{
    public partial class SendEmailWindow : Window
    {
        private GmailService _gmailService;
        private GraphService _graphService;
        private Provider _provider;

        public SendEmailWindow(GmailService gmailService, GraphService graphService, Provider provider)
        {
            InitializeComponent();
            _gmailService = gmailService;
            _graphService = graphService;
            _provider = provider;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var recipient = RecipientTextBox.Text;
            var subject = SubjectTextBox.Text;
            var body = BodyTextBox.Text;

            if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            bool error = false;

            try
            {
                await MailService.SendEmail(_provider, _graphService, _gmailService, recipient, subject, body);

                MessageBox.Show("Email sent successfully!");

            }
            catch (Exception ex)
            {
                error = true;
                MessageBox.Show($"Failed to send email: {ex.Message}");
            }

            if (!error)
            {
                this.Close(); // Cierra la ventana después de enviar
            }
        }
    }
}
