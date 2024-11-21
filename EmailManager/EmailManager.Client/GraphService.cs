using Microsoft.Graph.Models;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Net.Http;
using Folder = EmailManager.Client.Model.Folder;
using EmailManager.Client.Model;

namespace EmailManager.Client
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
            return folders.Value.Select(folder => new Folder
            {
                Id = folder.Id,
                Name = folder.DisplayName
            }).ToList();
        }

        public async Task<IEnumerable<Email>> GetEmailsFromFolderAsync(string folderId)
        {
            // Obtiene los correos electrónicos de una carpeta específica
            var emails = await _graphClient.Me.MailFolders[folderId].Messages
                .GetAsync();

            return emails.Value.Select(message => new Email
            {
                Subject = message.Subject,
                ReceivedDateTime = message.ReceivedDateTime,
                Sender = message.From?.EmailAddress?.Address ?? "Unknown"
            }).ToList();
        }
    }
}

