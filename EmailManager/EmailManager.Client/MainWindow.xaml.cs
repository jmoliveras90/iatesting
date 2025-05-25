using EmailManager.Library.Enum;
using EmailManager.Library.Model;
using EmailManager.Library.Services;
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
       // private Provider _currentProvider;
        private AuthService _authService;
        private GraphService _graphService;
        private List<AuthenticatedAccount> _authenticatedAccounts = new List<AuthenticatedAccount>();
        private AuthenticatedAccount _selectedAccount;

        public MainWindow()
        {
            InitializeComponent();
            var clientId = _configService.GetConfigValue("AzureAd:ClientId");
            var tenantId = _configService.GetConfigValue("AzureAd:TenantId");
            var redirectUri = _configService.GetConfigValue("AzureAd:RedirectUri");

            _authService = new AuthService(clientId, tenantId, redirectUri);
        }

        private void AccountsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedAccount = AccountsComboBox.SelectedItem as AuthenticatedAccount;

            if (_selectedAccount != null)
            {
                // Actualizar carpetas y correos según la cuenta seleccionada
                LoadFoldersAsync().ConfigureAwait(false);
            }
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var accountType = MessageBox.Show("¿Deseas añadir una cuenta de Google? (No = Microsoft)",
                                              "Añadir Cuenta",
                                              MessageBoxButton.YesNo);

            if (accountType == MessageBoxResult.Yes)
            {
                await AddGoogleAccountAsync();
            }
            else
            {
                await AddMicrosoftAccountAsync();
            }
        }

        private async Task AddGoogleAccountAsync()
        {

            try
            {               

                // Autenticación con Google
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = _configService.GetConfigValue("Google:ClientId"),
                        ClientSecret = _configService.GetConfigValue("Google:ClientSecret")
                    },
                    new[] { "https://www.googleapis.com/auth/userinfo.profile", GmailService.Scope.GmailSend, GmailService.Scope.GmailReadonly, GmailService.Scope.GmailLabels },
                    "user",
                    CancellationToken.None);

                 //  await credential.RevokeTokenAsync(CancellationToken.None);       


                _gmailService = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Email Manager"
                });

                MessageBox.Show("Logged in with Google successfully!");

                var userInfo = await _gmailService.Users.GetProfile("me").ExecuteAsync();



                _selectedAccount = new AuthenticatedAccount
                {
                    Email = userInfo.EmailAddress,
                    DisplayName = userInfo.EmailAddress, // Puedes añadir más detalles
                    Provider = Provider.Google
                };
                _authenticatedAccounts.Add(_selectedAccount);

                UpdateAccountsComboBox(_selectedAccount);
                await LoadFoldersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al añadir cuenta de Google: {ex.Message}");
            }
        }

        private async Task AddMicrosoftAccountAsync()
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync(["Mail.Read", "Mail.Send", "User.Read"]);
                _graphService = new GraphService(token);

                MessageBox.Show("Inicio de sesión exitoso.");

                var user = await _graphService.GetUserData();

                _selectedAccount = new AuthenticatedAccount
                {
                    Email = user.UserPrincipalName,
                    DisplayName = user.DisplayName,
                    Provider = Provider.Microsoft,
                };

                _authenticatedAccounts.Add(_selectedAccount);               

                UpdateAccountsComboBox(_selectedAccount);
                await LoadFoldersAsync();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al añadir cuenta de Microsoft: {ex.Message}");
            }
        }

        private void UpdateAccountsComboBox(AuthenticatedAccount newAccount = null)
        {
            AccountsComboBox.ItemsSource = null;
            AccountsComboBox.ItemsSource = _authenticatedAccounts;

            if (newAccount != null)
            {
                AccountsComboBox.SelectedItem = newAccount;
            }
        }


        private async void FoldersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedAccount == null) 
                return;

            var selectedFolder = FoldersList.SelectedItem as Folder;
            if (selectedFolder == null)
                return;

            try
            {
                EmailsList.ItemsSource = null;
                EmailsList.ItemsSource = await MailService.LoadEmailsAsync(_gmailService, _graphService, _selectedAccount.Provider, selectedFolder.Id);
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

                var content = await MailService.LoadEmailAsync(_gmailService, _graphService, _selectedAccount.Provider, selectedEmail.Id);

                if (_selectedAccount.Provider == Provider.Google)
                {
                    EmailContentWebBrowser.NavigateToString(content); // Cargar HTML en el navegador
                }
                else if (_selectedAccount.Provider == Provider.Microsoft)
                {
                    EmailContentWebBrowser.NavigateToString($"<html><body>{content}</body></html>");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load email content: {ex.Message}");
            }
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                FoldersList.ItemsSource = null;
                EmailsList.ItemsSource = null;

                FoldersList.ItemsSource = await MailService.LoadFoldersAsync(_gmailService, _graphService, _selectedAccount.Provider);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders: {ex.Message}");
            }
        }

        private void ComposeEmailButton_Click(object sender, RoutedEventArgs e)
        {
            var sendEmailWindow = new SendEmailWindow(_gmailService, _graphService, _selectedAccount.Provider);
            sendEmailWindow.Show();
        }
    }
}