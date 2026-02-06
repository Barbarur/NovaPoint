using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.Authentication
{
    public class AppInfo : IAppClient
    {
        private readonly LoggerSolution _logger;

        private string _adminUrl = string.Empty;
        public string AdminUrl
        {
            get { return _adminUrl; }
            set
            {
                _adminUrl = value;
                _logger.Info(GetType().Name, $"SPO -admin URL '{value}'");
            }
        }

        private string _rootPersonalUrl = string.Empty;
        public string RootPersonalUrl
        {
            get { return _rootPersonalUrl; }
            set
            {
                _rootPersonalUrl = value;
                _logger.Info(GetType().Name, $"SPO -my URL '{value}'");
            }
        }
        private string _rootSharedUrl = string.Empty;
        public string RootSharedUrl
        {
            get { return _rootSharedUrl; }
            set
            {
                _rootSharedUrl = value;
                _logger.Info(GetType().Name, $"SPO root URL '{value}'");
            }
        }
        private string _domain = string.Empty;
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

        internal AppSettings Settings { get; set; }
        public CancellationToken CancelToken { get; init; }

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly IPublicClientApplication _app;
        private AuthenticationResult? _graphAuthenticationResult = null;
        private AuthenticationResult? _adminAuthenticationResult = null;
        private AuthenticationResult? _rootPersonalAuthenticationResult = null;
        private AuthenticationResult? _rootSharedAuthenticationResult = null;

        internal AppInfo(LoggerSolution logger, CancellationTokenSource cancelTokenSource)
        {
            _logger = logger;

            Settings = AppSettings.GetSettings();
            Settings.ValidateSettings();

            this.CancelToken = cancelTokenSource.Token;

            Uri authority = new($"https://login.microsoftonline.com/{Settings.TenantID}");
            _app = PublicClientApplicationBuilder.Create(Settings.ClientId)
                                                 .WithAuthority(authority)
                                                 .WithDefaultRedirectUri()
                                                 .Build();
        }

        internal static async Task<AppInfo> BuildAsync(LoggerSolution logger, CancellationTokenSource cancelTokenSource)
        {
            AppInfo appInfo = new(logger, cancelTokenSource);
            appInfo.IsCancelled();

            string url = $"/sites/root";
            var graphSiteRoot = await new GraphAPIHandler(logger, appInfo).GetObjectAsync<GraphSitesRoot>(url);
            logger.Info("Appinfo", $"Hostname: {graphSiteRoot.SiteCollection.Hostname}");

            string domain = graphSiteRoot.SiteCollection.Hostname.Remove(graphSiteRoot.SiteCollection.Hostname.IndexOf(".sharepoint.com", StringComparison.OrdinalIgnoreCase));
            logger.Info("Appinfo", $"Domain: {domain}");

            appInfo.Domain = domain;

            return appInfo;
        }


        public void IsCancelled()
        {
            if ( CancelToken.IsCancellationRequested ) { CancelToken.ThrowIfCancellationRequested(); }
        }

        public async Task<string> GetGraphAccessToken()
        {
            this.IsCancelled();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            _graphAuthenticationResult = await GetAccessTokenFromMemory(_graphAuthenticationResult, scopes);
            AuthenticationResult? result = _graphAuthenticationResult;

            if (result != null)
            {
                _logger.Info(GetType().Name, $"Access Token expiration time: {result.ExpiresOn}");
                return result.AccessToken;
            }
            else
            {
                throw new Exception("Access Token could not be aquired");
            }
        }

        public async Task<ClientContext> GetContext(string siteUrl)
        {
            string accessToken = await GetSPOAccessToken(siteUrl);

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return clientContext;
        }

        public async Task<string> GetSPOAccessToken(string siteUrl)
        {
            this.IsCancelled();

            string rootUrl = siteUrl[..(siteUrl.IndexOf(".com") + 4)];
            string defaultPermissions = rootUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            _logger.Info(GetType().Name, $"Getting Access Token for root site {rootUrl}");

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
                _logger.Info(GetType().Name, $"Access Token expiration time: {result.ExpiresOn}");
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

            AuthenticationResult? result = null;

            if (cachedResult != null)
            {
                var timeNow = DateTime.UtcNow;
                var difference = cachedResult.ExpiresOn.Subtract(timeNow);

                if (difference.TotalMinutes > 10)
                {
                    result = cachedResult;
                }
            }

            result ??= await GetAccessToken(scopes);

            return result;
        }

        private async Task<AuthenticationResult?> GetAccessToken(string[] scopes)
        {
            this.IsCancelled();

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

            AuthenticationResult result;
            await _semaphore.WaitAsync();
            try
            {

                if (Settings.CachingToken)
                {
                    var cacheHelper = await TokenCacheHelper.GetCache();
                    cacheHelper.RegisterCache(_app.UserTokenCache);
                }

                try
                {
                    var accounts = await _app.GetAccountsAsync();
                    result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                .ExecuteAsync(CancelToken);
                }
                catch
                {
                    this.IsCancelled();

                    result = await _app.AcquireTokenInteractive(scopes)
                                      .WithUseEmbeddedWebView(false)
                                      .ExecuteAsync(CancelToken);
                }

            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

    }
}
