using Microsoft.Identity.Client;

namespace EmailManager.Client
{
    public class AuthService(string clientId, string tenantId, string redirectUri)
    {
        private readonly IPublicClientApplication _app = PublicClientApplicationBuilder.Create(clientId)
                     .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                     .WithRedirectUri(redirectUri)
                     .Build();

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
