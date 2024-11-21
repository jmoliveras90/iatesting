using EmailManager.Client.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Graph;
using System.Windows;
using System.Windows.Controls;

namespace EmailManager.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        private GmailService _gmailService;
        private ConfigService _configService = new ConfigService();
        private GraphServiceClient _microsoftGraphClient;
        private string _currentProvider;
        private AuthService _authService;
        private GraphService _graphService;

        public MainWindow()
        {
            InitializeComponent();
            var clientId = _configService.GetConfigValue("AzureAd:ClientId");
            var tenantId = _configService.GetConfigValue("AzureAd:TenantId");
            var redirectUri = _configService.GetConfigValue("AzureAd:RedirectUri");

            _authService = new AuthService(clientId, tenantId, redirectUri);
        }

        private async void FoldersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFolder = FoldersList.SelectedItem as Folder;
            if (selectedFolder == null)
                return;

            try
            {
                EmailsList.ItemsSource = null;

                if (_currentProvider == "Google")
                {
                    // Cargar correos de Google
                    await LoadEmailsFromGoogleAsync(selectedFolder.Id);
                }
                else if (_currentProvider == "Microsoft")
                {
                    // Cargar correos de Microsoft
                    await LoadEmailsFromMicrosoftAsync(selectedFolder.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load emails: {ex.Message}");
            }
        }
        private async void LoginGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentProvider = "Google";

                // Autenticación con Google
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = _configService.GetConfigValue("Google:ClientId"),
                        ClientSecret = _configService.GetConfigValue("Google:ClientSecret")
                    },
                    new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailLabels },
                    "user",
                    CancellationToken.None);

                _gmailService = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Email Manager"
                });

                MessageBox.Show("Logged in with Google successfully!");
                await LoadFoldersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Google login failed: {ex.Message}");
            }
        }

        private async void LoginMicrosoftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentProvider = "Microsoft";
                // Inicia sesión y configura el servicio Graph
                var token = await _authService.GetAccessTokenAsync(new[] { "Mail.Read" });
                _graphService = new GraphService(token);

                MessageBox.Show("Inicio de sesión exitoso.");

                await LoadFoldersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el inicio de sesión: {ex.Message}");
            }
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                FoldersList.ItemsSource = null;
                EmailsList.ItemsSource = null;

                if (_currentProvider == "Google")
                {
                    // Cargar carpetas de Google
                    var labels = await _gmailService.Users.Labels.List("me").ExecuteAsync();
                    FoldersList.ItemsSource = labels.Labels.Select(label => new Folder
                    {
                        Id = label.Id,
                        Name = label.Name
                    }).ToList();
                }
                else if (_currentProvider == "Microsoft")
                {
                    // Cargar carpetas de Microsoft
                    var mailFolders = await _graphService.GetMailFoldersAsync();                   

                    FoldersList.ItemsSource = mailFolders.Select(folder => new Folder
                    {
                        Id = folder.Id,
                        Name = folder.Name
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }
        }

        private async Task LoadEmailsFromGoogleAsync(string folderId)
        {
            var request = _gmailService.Users.Messages.List("me");
            request.LabelIds = new List<string> { folderId };
            request.MaxResults = 20;

            var response = await request.ExecuteAsync();
            var emails = new List<Email>();

            if (response.Messages != null)
            {
                foreach (var message in response.Messages)
                {
                    var email = await _gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
                    emails.Add(new Email
                    {
                        Subject = email.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value,
                        Sender = email.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value,
                        ReceivedDateTime = email.InternalDate.HasValue
                            ? DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value)
                            : null
                    });
                }
            }

            EmailsList.ItemsSource = emails;
        }

        private async Task LoadEmailsFromMicrosoftAsync(string folderId)
        {
            var messages = await _graphService.GetEmailsFromFolderAsync(folderId);

            EmailsList.ItemsSource = messages;
        }
    }
}