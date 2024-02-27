using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Framework.Modernization.Functions;
using System.Reflection;

namespace NovaPointLibrary.Commands.Authentication
{
    public class AppInfo
    {
        private readonly NPLogger _logger;

        private string _adminUrl = string.Empty;
        internal string AdminUrl
        {
            get { return _adminUrl; }
            set
            {
                _adminUrl = value;
                _logger.LogTxt(GetType().Name, $"SPO Admin URL '{value}'");
            }
        }

        private string _rootPersonalUrl = string.Empty;
        internal string RootPersonalUrl
        {
            get { return _rootPersonalUrl; }
            set
            {
                _rootPersonalUrl = value;
                _logger.LogTxt(GetType().Name, $"SPO -my URL '{value}'");
            }
        }
        private string _rootSharedUrl = string.Empty;
        internal string RootSharedUrl
        {
            get { return _rootSharedUrl; }
            set
            {
                _rootSharedUrl = value;
                _logger.LogTxt(GetType().Name, $"SPO root URL '{value}'");
            }
        }
        private string _domain = String.Empty;
        internal string Domain
        {
            get { return _domain; }
            set
            {
                _domain = value;
                AdminUrl = "https://" + value + "-admin.sharepoint.com";
                RootPersonalUrl = "https://" + value + "-my.sharepoint.com";
                RootSharedUrl = "https://" + value + ".sharepoint.com";
            }
        }

        internal string _tenantId { get; set; } = string.Empty;
        private string _clientId;
        private bool _cachingToken;

        internal CancellationTokenSource CancelTokenSource { get; init; }
        internal CancellationToken CancelToken { get; init; }

        private readonly IPublicClientApplication _app;
        private AuthenticationResult? _graphAuthenticationResult = null;
        private AuthenticationResult? _adminAuthenticationResult = null;
        private AuthenticationResult? _rootPersonalAuthenticationResult = null;
        private AuthenticationResult? _rootSharedAuthenticationResult = null;

        internal AppInfo(NPLogger logger, CancellationTokenSource cancelTokenSource)
        {
            _logger = logger;

            var appSettings = AppSettings.GetSettings();
            Domain = appSettings.Domain;
            _tenantId = appSettings.TenantID;
            _clientId = appSettings.ClientId;
            _cachingToken = appSettings.CachingToken;

            if (string.IsNullOrWhiteSpace(Domain) || string.IsNullOrWhiteSpace(_tenantId) || string.IsNullOrWhiteSpace(_clientId))
            {
                throw new Exception("Please go to Settings and fill the App Information");
            }

            this.CancelTokenSource = cancelTokenSource;
            this.CancelToken = cancelTokenSource.Token;

            Uri authority = new($"https://login.microsoftonline.com/{_tenantId}");
            _app = PublicClientApplicationBuilder.Create(_clientId)
                                                 .WithAuthority(authority)
                                                 .WithDefaultRedirectUri()
                                                 .Build();
        }

        internal void IsCancelled()
        {
            if ( CancelToken.IsCancellationRequested ) { CancelToken.ThrowIfCancellationRequested(); }
        }
        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }

        internal async Task<string> GetGraphAccessToken()
        {
            this.IsCancelled();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            _graphAuthenticationResult = await GetAccessTokenFromMemory(_graphAuthenticationResult, scopes);
            AuthenticationResult? result = _graphAuthenticationResult;

            if (result != null)
            {
                _logger.LogTxt(GetType().Name, $"Access Token expiration time: {result.ExpiresOn}");
                return result.AccessToken;
            }
            else
            {
                throw new Exception("Access Token could not be aquired");
            }
        }

        internal async Task<ClientContext> GetContext(string siteUrl)
        {
            string accessToken = await GetSPOAccessToken(siteUrl);

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return clientContext;
        }

        internal async Task<string> GetSPOAccessToken(string siteUrl)
        {
            this.IsCancelled();

            string rootUrl = siteUrl[..(siteUrl.IndexOf(".com") + 4)];
            string defaultPermissions = rootUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            _logger.LogTxt(GetType().Name, $"Start getting Access Token for root site {rootUrl}");

            AuthenticationResult? result = null;
            if (rootUrl.Equals(AdminUrl, StringComparison.OrdinalIgnoreCase))
            {
                _adminAuthenticationResult = await GetAccessTokenFromMemory(_adminAuthenticationResult, scopes);
                result = _adminAuthenticationResult;
            }
            else if (rootUrl.Equals(RootSharedUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootSharedAuthenticationResult = await GetAccessTokenFromMemory(_rootSharedAuthenticationResult, scopes);
                result = _rootSharedAuthenticationResult;
            }
            else if (rootUrl.Equals(RootPersonalUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootPersonalAuthenticationResult = await GetAccessTokenFromMemory(_rootPersonalAuthenticationResult, scopes);
                result = _rootPersonalAuthenticationResult;
            }
            else
            {
                throw new Exception($"root site '{rootUrl}' from '{siteUrl}' does not match with admin '{AdminUrl}', neither root urls '{RootSharedUrl}' '{RootPersonalUrl}'");
            }

            if (result != null)
            {
                _logger.LogTxt(GetType().Name, $"Access Token expiration time: {result.ExpiresOn}");
                return result.AccessToken;
            }
            else
            {
                throw new Exception("Access Token could not be aquired");
            }
        }

        private async Task<AuthenticationResult?> GetAccessTokenFromMemory(AuthenticationResult? cachedResult, string[] scopes)
        {
            this.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Access Token from memory");

            AuthenticationResult? result = null;

            if (cachedResult != null)
            {
                var timeNow = DateTime.UtcNow;
                var difference = cachedResult.ExpiresOn.Subtract(timeNow);

                if (difference.TotalMinutes > 10)
                {
                    _logger.LogTxt(GetType().Name, $"Got access token from memory");
                    result = cachedResult;
                }
            }

            result ??= await GetAccessToken(scopes);

            return result;
        }

        private async Task<AuthenticationResult?> GetAccessToken(string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetAccessToken";
            _logger.LogTxt(methodName, $"Start getting Access Token");

            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
            var aquireToken = AcquireTokenInteractiveAsync(scopes);

            TaskCompletionSource taskCompletionSource = new();

            CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(aquireToken, taskCompletionSource.Task);

            if (completedTask != aquireToken || CancelToken.IsCancellationRequested)
            {
                CancelToken.ThrowIfCancellationRequested();
                return null;
            }
            else
            {
                return await aquireToken;
            }
        }

        private async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetTokenDinamicaly";
            _logger.LogTxt(methodName, $"Start aquiring Access Token");

            if (_cachingToken)
            {
                _logger.LogTxt(methodName, "Adding cached access token");

                var cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(_app.UserTokenCache);
            }

            AuthenticationResult result;
            try
            {
                _logger.LogTxt(methodName, $"Start aquiring Access Token from Cache");

                var accounts = await _app.GetAccountsAsync();
                result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                _logger.LogTxt(methodName, $"Finish aquiring Access Token from Cache");
            }
            catch (MsalUiRequiredException ex)
            {
                if (this.CancelToken.IsCancellationRequested) { this.CancelToken.ThrowIfCancellationRequested(); };
                _logger.LogTxt(methodName, ex.Message);
                _logger.LogTxt(methodName, $"{ex.StackTrace}");
                _logger.LogTxt(methodName, $"Start aquiring new Access Token from AAD");

                result = await _app.AcquireTokenInteractive(scopes)
                                  .WithUseEmbeddedWebView(false)
                                  .ExecuteAsync();

                _logger.LogTxt(methodName, $"Finish aquiring new Access Token from AAD");
            }
            catch (MsalServiceException ex)
            {
                _logger.LogTxt(methodName, $"FAILED aquiring new Access Token from AAD");
                _logger.LogTxt(methodName, ex.Message);
                _logger.LogTxt(methodName, $"{ex.StackTrace}");
                throw;
            }

            return result;
        }
    }
}
