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

            if (folders == null || folders.Value == null || folders.Value.Count == 0)
            {
                return [];
            }

            return folders.Value.Select(folder => new Folder
            {
                Id = folder.Id ?? string.Empty,
                Name = folder.DisplayName ?? string.Empty
            }).ToList() ?? [];
        }

        public async Task<IEnumerable<Email>> GetEmailsFromFolderAsync(string folderId)
        {
            // Obtiene los correos electrónicos de una carpeta específica
            var emails = await _graphClient.Me.MailFolders[folderId].Messages
            .GetAsync();

            if (emails == null || emails.Value == null || emails.Value.Count == 0)
            {
                return [];
            }

            return emails.Value.Select(message => new Email
            {
                Subject = message.Subject ?? string.Empty,
                ReceivedDateTime = message.ReceivedDateTime,
                Sender = message.From?.EmailAddress?.Address ?? "Unknown"
            }).ToList();
        }
    }
}

