using EmailManager.Client.Enum;
using EmailManager.Client.Model;
using Google.Apis.Gmail.v1;
using Microsoft.Graph.Models;
using System.Windows;
using Folder = EmailManager.Client.Model.Folder;

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
    }
}
