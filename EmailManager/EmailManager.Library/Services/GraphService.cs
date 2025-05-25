using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Net.Http;
using Folder = EmailManager.Library.Model.Folder;
using EmailManager.Library.Model;
using System.Windows;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;


namespace EmailManager.Library.Services
{
    public class GraphService
    {
        private readonly GraphServiceClient _graphClient;

        public GraphService(string accessToken)
        {
            _graphClient = new GraphServiceClient(new HttpClient
            {
                DefaultRequestHeaders =            
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
                }
            });
        }

        public async Task<IEnumerable<Folder>> GetMailFoldersAsync()
        {
            var folders = await _graphClient.Me.MailFolders
                                           .GetAsync();

            if (folders == null || folders.Value == null || folders.Value.Count == 0)
            {
                return new List<Folder>();
            }

            return folders.Value.Select(folder => new Folder
            {
                Id = folder.Id ?? string.Empty,
                Name = folder.DisplayName ?? string.Empty
            }).ToList() ?? new List<Folder>();
        }

        public async Task<IEnumerable<Email>> GetEmailsFromFolderAsync(string folderId)
        {
            // Obtiene los correos electrónicos de una carpeta específica
            var emails = await _graphClient.Me.MailFolders[folderId].Messages
            .GetAsync();

            if (emails == null || emails.Value == null || emails.Value.Count == 0)
            {
                return new List<Email>();
            }

            return emails.Value.Select(message => new Email
            {
                Id = message.Id ?? string.Empty,
                Subject = message.Subject ?? string.Empty,
                ReceivedDateTime = message.ReceivedDateTime,
                Sender = message.From?.EmailAddress?.Address ?? "Unknown"
            }).ToList();
        }

        public async Task<string> GetEmail(string emailId)
        {
            var mail = await _graphClient.Me.Messages[emailId].GetAsync();

            return mail?.Body?.Content ?? string.Empty;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {

                var requestBody = new SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Text,
                            Content = body
                        },
                        ToRecipients = new List<Recipient>
                        {
                            new Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = recipient,
                                },
                            },
                        }
                    }
                };

                // Enviar el correo
                await _graphClient.Me
                    .SendMail
                    .PostAsync(requestBody);

               //  MessageBox.Show("Email sent successfully (Microsoft)!");
            }
            catch (Exception ex)
            {
             //   MessageBox.Show($"Failed to send email (Microsoft): {ex.Message}");
                throw;
            }
        }

        public async Task<User?> GetUserData()
        {
            return await _graphClient.Me.GetAsync();
        }
    }
}

