using Microsoft.Identity.Client;
using System.Linq;
using System.Threading.Tasks;

namespace EmailManager.Client
{
    public class AuthService
    {
        private readonly IPublicClientApplication _app;
        public readonly string _clientId;
        public readonly string _tenantId;
        public readonly string _redirectUri;

        public AuthService(string clientId, string tenantId, string redirectUri)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _redirectUri = redirectUri;

            _app = PublicClientApplicationBuilder.Create(_clientId)
                     .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
                     .WithRedirectUri(_redirectUri)
                     .Build();
        }                

        public async Task<string> GetAccessTokenAsync(string[] scopes)
        {
            var accounts = await _app.GetAccountsAsync();

            try
            {
                var result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                       .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                var result = await _app.AcquireTokenInteractive(scopes)
                                       .WithPrompt(Prompt.SelectAccount)
                                       .ExecuteAsync();
                return result.AccessToken;
            }
        }
    }
}
