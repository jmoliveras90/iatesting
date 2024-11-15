using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EmailManager.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AuthService _authService;
        private GraphService _graphService;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializa el servicio de autenticación
            var configService = new ConfigService();
            var clientId = configService.GetConfigValue("AzureAd:ClientId");
            var tenantId = configService.GetConfigValue("AzureAd:TenantId");
            var redirectUri = configService.GetConfigValue("AzureAd:RedirectUri");

            _authService = new AuthService(clientId, tenantId, redirectUri);

            // Cargar carpetas al iniciar la aplicación
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    // Autenticación y configuración del servicio Graph
            //    var token = await _authService.GetAccessTokenAsync(new[] { "Mail.Read" });
            //    _graphService = new GraphService(token);

            //    // Cargar carpetas del buzón
            //    var folders = await _graphService.GetMailFoldersAsync();
            //    FoldersList.ItemsSource = folders.Select(f => new { f.Id, f.DisplayName });
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Error al cargar carpetas: {ex.Message}");
            //}
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Inicia sesión y configura el servicio Graph
                var token = await _authService.GetAccessTokenAsync(new[] { "Mail.Read" });
                _graphService = new GraphService(token);

                MessageBox.Show("Inicio de sesión exitoso.");

                // Cargar carpetas del buzón
                var folders = await _graphService.GetMailFoldersAsync();
                FoldersList.ItemsSource = folders.Select(f => new { f.Id, f.DisplayName });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el inicio de sesión: {ex.Message}");
            }
        }

        private async void FoldersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                // Obtener la carpeta seleccionada
                var selectedFolder = FoldersList.SelectedItem as dynamic;
                if (selectedFolder == null) return;

                // Obtener los correos electrónicos de la carpeta seleccionada
                var folderId = selectedFolder.Id;
                var emails = await _graphService.GetEmailsFromFolderAsync(folderId);

                // Mostrar los correos en la lista
                EmailsList.ItemsSource = emails;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar correos: {ex.Message}");
            }
        }

    }
}