using EmailManager.Client.Enum;
using EmailManager.Client.Model;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using MimeKit;
using System.Windows;
using Folder = EmailManager.Client.Model.Folder;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;

namespace EmailManager.Client
{
    public static class MailService
    {
        public static async Task<IEnumerable<Folder>> LoadFoldersAsync(GmailService gmailService,
            GraphService graphService, Provider provider)
        {
            try
            {
                switch (provider)
                {
                    case Provider.Google:
                        {
                            // Cargar carpetas de Google
                            var labels = await gmailService.Users.Labels.List("me").ExecuteAsync();
                            return labels.Labels.Select(label => new Folder
                            {
                                Id = label.Id,
                                Name = label.Name
                            }).ToList();
                        }

                    case Provider.Microsoft:
                        {
                            // Cargar carpetas de Microsoft
                            var mailFolders = await graphService.GetMailFoldersAsync();

                            return mailFolders.Select(folder => new Folder
                            {
                                Id = folder.Id,
                                Name = folder.Name
                            }).ToList();
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }

            return [];
        }

        public static async Task<IEnumerable<Email>> LoadEmailsAsync(GmailService gmailService,
        GraphService graphService, Provider provider, string folderId)
        {
            List<Email> result = [];

            try
            {
                switch (provider)
                {
                    case Provider.Google:
                        {
                            var request = gmailService.Users.Messages.List("me");
                            request.LabelIds = new List<string> { folderId };
                            request.MaxResults = 20;

                            var response = await request.ExecuteAsync();

                            if (response.Messages != null)
                            {
                                foreach (var message in response.Messages)
                                {
                                    var email = await gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
                                    result.Add(new Email
                                    { 
                                        Id = email.Id,
                                        Subject = email.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? string.Empty,
                                        Sender = email.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value ?? string.Empty,
                                        ReceivedDateTime = email.InternalDate.HasValue
                                            ? DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value)
                                            : null
                                    });
                                }
                            }
                            break;
                        }
                    case Provider.Microsoft:
                        result = (await graphService.GetEmailsFromFolderAsync(folderId)).ToList();
                        break;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }

            return result;
        }

        public static async Task<string> LoadEmailAsync(GmailService gmailService,
        GraphService graphService, Provider provider, string emailId)
        {
            List<Email> result = [];

            try
            {
                switch (provider)
                {
                    case Provider.Google:
                        {
                            var email = await gmailService.Users.Messages.Get("me", emailId).ExecuteAsync();

                            // Buscar el contenido del correo
                            var part = email.Payload?.Parts?.FirstOrDefault(p => p.MimeType == "text/html");

                            if (part?.Body?.Data != null)
                            {
                                var contentBytes = Convert.FromBase64String(part.Body.Data.Replace('-', '+').Replace('_', '/'));
                                return System.Text.Encoding.UTF8.GetString(contentBytes);
                            }

                            return string.Empty;
                        }
                    case Provider.Microsoft:
                        return await graphService.GetEmail(emailId);      
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }

            return string.Empty;
        }

        public static async Task SendEmail(Provider provider, GraphService graphService, GmailService gmailService, string recipient, string subject, string body)
        {
            switch (provider)
            {
                case Provider.Google:
                    {
                        await SendEmailViaGmailAsync(gmailService, recipient, subject, body);
                    }
                    break;
                case Provider.Microsoft:
                    {
                        await graphService.SendEmailAsync(recipient, subject, body);
                    }
                    break;
            }
        }

        private static async Task SendEmailViaGmailAsync(GmailService gmailService, string recipient, string subject, string body)
        {
            try
            {
                var peopleService = new PeopleServiceService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = gmailService.HttpClientInitializer,
                    ApplicationName = "Email Manager"
                });

                var request = peopleService.People.Get("people/me");
                request.PersonFields = "names,emailAddresses"; // Define los campos que necesitas

                var profile = await request.ExecuteAsync();

                // Accede al nombre del usuario
                var name = profile.Names?.FirstOrDefault()?.DisplayName;        
                var email = profile.EmailAddresses?.FirstOrDefault()?.Value;

                var mimeMessage = new MimeKit.MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(name, string.Empty));
                mimeMessage.To.Add(new MimeKit.MailboxAddress("", recipient));
                mimeMessage.Subject = subject;

                mimeMessage.Body = new MimeKit.TextPart("html")
                {
                    Text = body
                };

                // Convertir el mensaje MIME a base64 URL-safe
                var rawMessage = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mimeMessage.ToString()))
                    .Replace('+', '-').Replace('/', '_').Replace("=", "");

                // Enviar el correo
                var message = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };
                await gmailService.Users.Messages.Send(message, "me").ExecuteAsync();

               // MessageBox.Show("Email sent successfully (Gmail)!");
            }
            catch (Exception ex)
            {
                throw;
              //  MessageBox.Show($"Failed to send email (Gmail): {ex.Message}");
            }
        }
    }
}
