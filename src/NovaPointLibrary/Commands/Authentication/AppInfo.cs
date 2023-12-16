using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Framework.Modernization.Functions;

namespace NovaPointLibrary.Commands.Authentication
{
    public class AppInfo
    {
        internal string _domain = String.Empty;
        internal string AdminUrl { get; set; } = String.Empty;
        internal string RootPersonalUrl { get; set; } = String.Empty;
        internal string RootSharedUrl { get; set; } = String.Empty;
        public string Domain
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
        
        internal string _clientId { get; set; } = string.Empty;
        
        internal bool _cachingToken { get; set; } = false;

        public CancellationTokenSource CancelTokenSource { get; init; }
        internal CancellationToken CancelToken { get; init; }

        private readonly IPublicClientApplication _app;
        private AuthenticationResult? _adminAuthenticationResult = null;
        private AuthenticationResult? _rootPersonalAuthenticationResult = null;
        private AuthenticationResult? _rootSharedAuthenticationResult = null;

        // KEEP FOR TESTING EASY SWITCH BETWEEN TENANTS
        public AppInfo(string domain, string tenantId, string clientId, bool cachingToken)
        {
            Domain = domain;
            _tenantId = tenantId;
            _clientId = clientId;
            _cachingToken = cachingToken;

            this.CancelTokenSource = new();
            this.CancelToken = CancelTokenSource.Token;


            Uri authority = new($"https://login.microsoftonline.com/{_tenantId}");
            _app = PublicClientApplicationBuilder.Create(_clientId)
                                                 .WithAuthority(authority)
                                                 .WithDefaultRedirectUri()
                                                 .Build();
        }

        public AppInfo()
        {
            var appSettings = AppSettings.GetSettings();
            Domain = appSettings.Domain;
            _tenantId = appSettings.TenantID;
            _clientId = appSettings.ClientId;
            _cachingToken = appSettings.CachingToken;

            if(string.IsNullOrWhiteSpace(Domain) || string.IsNullOrWhiteSpace(_tenantId) || string.IsNullOrWhiteSpace(_clientId))
            {
                throw new Exception("Please go to Settings and fill the App Information");
            }

            this.CancelTokenSource = new();
            this.CancelToken = CancelTokenSource.Token;

            Uri authority = new($"https://login.microsoftonline.com/{_tenantId}");
            _app = PublicClientApplicationBuilder.Create(_clientId)
                                                 .WithAuthority(authority)
                                                 .WithDefaultRedirectUri()
                                                 .Build();
        }

        public void IsCancelled()
        {
            if ( CancelToken.IsCancellationRequested ) { CancelToken.ThrowIfCancellationRequested(); }
        }
        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }

        internal async Task<ClientContext> GetContext(NPLogger logger, string siteUrl)
        {
            string accessToken = await GetSPOAccessToken(logger, siteUrl);

            var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return clientContext;
        }

        internal async Task<string> GetSPOAccessToken(NPLogger logger, string siteUrl)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetSPOAccessToken";

            string rootUrl = siteUrl[..(siteUrl.IndexOf(".com") + 4)];
            string defaultPermissions = rootUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            logger.LogTxt(methodName, $"Start getting Access Token for root site {rootUrl}");

            AuthenticationResult? result = null;
            if (rootUrl.Equals(AdminUrl, StringComparison.OrdinalIgnoreCase))
            {
                _adminAuthenticationResult = await GetAccessTokenFromMemory(logger, _adminAuthenticationResult, scopes);
                result = _adminAuthenticationResult;
            }
            else if (rootUrl.Equals(RootSharedUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootSharedAuthenticationResult = await GetAccessTokenFromMemory(logger, _rootSharedAuthenticationResult, scopes);
                result = _rootSharedAuthenticationResult;
            }
            else if (rootUrl.Equals(RootPersonalUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootPersonalAuthenticationResult = await GetAccessTokenFromMemory(logger, _rootPersonalAuthenticationResult, scopes);
                result = _rootPersonalAuthenticationResult;
            }

            if (result != null)
            {
                logger.LogTxt(methodName, $"Access Token expiration time: {result.ExpiresOn}");
                return result.AccessToken;
            }
            else
            {
                throw new Exception("Access Token could not be aquired");
            }
        }

        private async Task<AuthenticationResult?> GetAccessTokenFromMemory(NPLogger logger, AuthenticationResult? cachedResult, string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetAccessTokenFromMemory";
            logger.LogTxt(methodName, $"Start getting Access Token from memory");

            AuthenticationResult? result = null;

            if (cachedResult != null)
            {
                var timeNow = DateTime.UtcNow;
                var difference = cachedResult.ExpiresOn.Subtract(timeNow);

                if (difference.TotalMinutes > 10)
                {
                    logger.LogTxt(methodName, $"Got access token from memory");
                    result = cachedResult;
                }
            }

            result ??= await GetAccessToken(logger, scopes);

            return result;
        }

        private async Task<AuthenticationResult?> GetAccessToken(NPLogger logger, string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetAccessToken";
            logger.LogTxt(methodName, $"Start getting Access Token");

            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
            var aquireToken = AcquireTokenInteractiveAsync(logger, scopes);

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
                logger.LogTxt(methodName, $"Finish getting Access Token");
                return await aquireToken;
            }
        }

        private async Task<AuthenticationResult> AcquireTokenInteractiveAsync(NPLogger logger, string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetTokenDinamicaly";
            logger.LogTxt(methodName, $"Start aquiring Access Token");

            if (_cachingToken)
            {
                logger.LogTxt(methodName, "Adding cached access token");

                var cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(_app.UserTokenCache);
            }

            AuthenticationResult result;
            try
            {
                logger.LogTxt(methodName, $"Start aquiring Access Token from Cache");

                var accounts = await _app.GetAccountsAsync();
                result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                logger.LogTxt(methodName, $"Finish aquiring Access Token from Cache");
            }
            catch (MsalUiRequiredException ex)
            {
                if (this.CancelToken.IsCancellationRequested) { this.CancelToken.ThrowIfCancellationRequested(); };
                logger.LogTxt(methodName, ex.Message);
                logger.LogTxt(methodName, $"{ex.StackTrace}");
                logger.LogTxt(methodName, $"Start aquiring new Access Token from AAD");

                result = await _app.AcquireTokenInteractive(scopes)
                                  .WithUseEmbeddedWebView(false)
                                  .ExecuteAsync();

                logger.LogTxt(methodName, $"Finish aquiring new Access Token from AAD");
            }
            catch (MsalServiceException ex)
            {
                logger.LogTxt(methodName, $"FAILED aquiring new Access Token from AAD");
                logger.LogTxt(methodName, ex.Message);
                logger.LogTxt(methodName, $"{ex.StackTrace}");
                throw;
            }

            return result;
        }
    }
}
