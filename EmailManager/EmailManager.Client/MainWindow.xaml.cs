using EmailManager.Client.Enum;
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
        private Provider _currentProvider;
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
                EmailsList.ItemsSource = await MailService.LoadEmailsAsync(_gmailService, _graphService, _currentProvider, selectedFolder.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load emails: {ex.Message}");
            }
        }

        private async void EmailsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedEmail = EmailsList.SelectedItem as Email;
            if (selectedEmail == null)
                return;

            try
            {

                var content = await MailService.LoadEmailAsync(_gmailService, _graphService, _currentProvider, selectedEmail.Id);

                if (_currentProvider == Provider.Google)
                {
                    EmailContentWebBrowser.NavigateToString(content); // Cargar HTML en el navegador
                }
                else if (_currentProvider == Provider.Microsoft)
                {
                    EmailContentWebBrowser.NavigateToString($"<html><body>{content}</body></html>");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load email content: {ex.Message}");
            }
        }


        private async void LoginGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentProvider = Provider.Google;

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
                _currentProvider = Provider.Microsoft;
                // Inicia sesión y configura el servicio Graph
                var token = await _authService.GetAccessTokenAsync(["Mail.Read"]);
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

                FoldersList.ItemsSource = await MailService.LoadFoldersAsync(_gmailService, _graphService, _currentProvider);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }
        }
    }
}