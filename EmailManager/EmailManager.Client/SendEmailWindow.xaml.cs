using EmailManager.Client.Enum;
using Google.Apis.Gmail.v1;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EmailManager.Client
{
    public partial class SendEmailWindow : Window
    {
        private GmailService _gmailService;
        private GraphServiceClient _microsoftGraphClient;

        public SendEmailWindow(GmailService gmailService, GraphServiceClient microsoftGraphClient)
        {
            InitializeComponent();
            _gmailService = gmailService;
            _microsoftGraphClient = microsoftGraphClient;
        }

        private async void SendViaMicrosoft_Click(object sender, RoutedEventArgs e)
        {
            var recipient = RecipientTextBox.Text;
            var subject = SubjectTextBox.Text;
            var body = BodyTextBox.Text;

            if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {


                MessageBox.Show("Email sent successfully (Microsoft)!");
                this.Close(); // Cierra la ventana después de enviar
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send email (Microsoft): {ex.Message}");
            }
        }

        private async void SendViaGmail_Click(object sender, RoutedEventArgs e)
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
                await MailService.SendEmail(Provider.Google, _gmailService, recipient, subject, body);
               
                MessageBox.Show("Email sent successfully (Gmail)!");
               
            }
            catch (Exception ex)
            {
                error = true;
                MessageBox.Show($"Failed to send email (Gmail): {ex.Message}");
            }

            if (!error)
            {
                this.Close(); // Cierra la ventana después de enviar
            }
        }
    }
}
