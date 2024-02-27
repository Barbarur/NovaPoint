//using CamlBuilder;
//using Microsoft.Identity.Client;
//using Microsoft.Identity.Client.Extensions.Msal;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.Authentication
//{
//    // TO BE DEPRECATED
//    internal class GetAccessToken
//    {
//        private readonly NPLogger _logger;
//        private readonly AppInfo _appInfo;

//        private readonly string _clientId = string.Empty;
//        private readonly  Uri? _authority;
//        private readonly bool _cachingToken;
//        private readonly string redirectUri = "http://localhost";

//        internal GetAccessToken(NPLogger logger, AppInfo appInfo)
//        {
//            _logger = logger;
//            _appInfo = appInfo;

//            _authority = new Uri($"https://login.microsoftonline.com/{appInfo._tenantId}");
//            _clientId = appInfo._clientId;
//            _cachingToken = appInfo._cachingToken;

//        }

//        internal async Task<string> GraphInteractiveAsync()
//        {

//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GraphInteractiveAsync";
//            _logger.LogTxt(methodName, $"Start getting Graph Access Token");

//            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
//            var aquireGraphToken = GraphInteractiveAquireTokenAsync();

//            TaskCompletionSource taskCompletionSource = new();

//            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

//            var completedTask = await Task.WhenAny(aquireGraphToken, taskCompletionSource.Task);

//            if (completedTask != aquireGraphToken || _appInfo.CancelToken.IsCancellationRequested)
//            {
//                _appInfo.CancelToken.ThrowIfCancellationRequested();
//                return null;
//            }
//            else
//            {
//                return await aquireGraphToken;
//            }

//        }

//        private async Task<string> GraphInteractiveAquireTokenAsync()
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GraphInteractiveAquireTokenAsync";
//            _logger.LogTxt(methodName, $"Start aquiring Graph Access Token");

//            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

//            var app = PublicClientApplicationBuilder.Create(_clientId)
//                                                    .WithAuthority(_authority)
//                                                    .WithRedirectUri(redirectUri)
//                                                    .Build();

//            if (_cachingToken)
//            {
//                _logger.LogTxt(methodName, $"Adding UserTokenCache");

//                MsalCacheHelper cacheHelper = await TokenCacheHelper.GetCache();
//                cacheHelper.RegisterCache(app.UserTokenCache);
//            }

//            AuthenticationResult result;
//            try
//            {
//                var accounts = await app.GetAccountsAsync();
//                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
//                            .ExecuteAsync();
//            }
//            catch (MsalUiRequiredException)
//            {
//                result = await app.AcquireTokenInteractive(scopes)
//                            .WithUseEmbeddedWebView(false)
//                            .ExecuteAsync();
//            }

//            _logger.LogTxt(methodName, $"Finish aquiring Graph Access Token");
//            return result.AccessToken;
//        }

//        // TO BE DEPRECATED
//        internal async Task<string> Graph_Interactive()
//        {
//            string[] scopes = new string[] { "https://graph.microsoft.com/Sites.FullControl.All" };

//            var app = PublicClientApplicationBuilder.Create(_clientId)
//                                                    .WithAuthority(_authority)
//                                                    .WithRedirectUri(redirectUri)
//                                                    .Build();

//            MsalCacheHelper cacheHelper = await TokenCacheHelper.GetCache();
//            cacheHelper.RegisterCache(app.UserTokenCache);

//            AuthenticationResult result;
//            try
//            {
//                var accounts = await app.GetAccountsAsync();
//                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
//                            .ExecuteAsync();
//            }
//            catch (MsalUiRequiredException)
//            {
//                result = await app.AcquireTokenInteractive(scopes)
//                            .WithUseEmbeddedWebView(false)
//                            .ExecuteAsync();
//            }
//            return result.AccessToken;

//        }

//        internal async Task<string> SpoInteractiveAsync(string siteUrl)
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.SpoInteractiveAsync";
//            _logger.LogTxt(methodName, $"Start getting SPO Access Token for '{siteUrl}'");

//            string defaultPermissions = siteUrl + "/.default";
//            string[] scopes = new string[] { defaultPermissions };

//            _logger.LogTxt(methodName, "Building App");
//            var app = PublicClientApplicationBuilder.Create(_clientId)
//                                                    .WithAuthority(_authority)
//                                                    .WithRedirectUri(redirectUri)
//                                                    .Build();

//            if (_cachingToken)
//            {
//                _logger.LogTxt(methodName, "Adding UserTokenCache");

//                var cacheHelper = await TokenCacheHelper.GetCache();
//                cacheHelper.RegisterCache(app.UserTokenCache);
//            }

//            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
//            var aquireToken = SpoInteractiveAcquireTokenAsync(app, scopes);

//            TaskCompletionSource taskCompletionSource = new();

//            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

//            var completedTask = await Task.WhenAny(aquireToken, taskCompletionSource.Task);

//            if (completedTask != aquireToken || _appInfo.CancelToken.IsCancellationRequested )
//            {
//                _appInfo.CancelToken.ThrowIfCancellationRequested();
//                return null;
//            }
//            else
//            {
//                _logger.LogTxt(methodName, $"Finish getting SPO Access Token for '{siteUrl}'");
//                return await aquireToken;
//            }

//        }

//        private async Task<string> SpoInteractiveAcquireTokenAsync(IPublicClientApplication app, string[] scopes)
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.SpoInteractiveAcquireTokenAsync";
//            _logger.LogTxt(methodName, $"Start aquiring SPO Access Token");

//            AuthenticationResult result;
//            try
//            {
//                _logger.LogTxt(methodName, $"Start aquiring Access Token from Cache");

//                var accounts = await app.GetAccountsAsync();
//                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
//                            .ExecuteAsync();

//                _logger.LogTxt(methodName, $"Finish aquiring Access Token from Cache");

//                return result.AccessToken;
//            }
//            catch (MsalUiRequiredException ex)
//            {
//                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
//                _logger.LogTxt(methodName, ex.Message);
//                _logger.LogTxt(methodName, $"{ex.StackTrace}");
//                _logger.LogTxt(methodName, $"Start aquiring new Access Token from AAD");

//                result = await app.AcquireTokenInteractive(scopes)
//                            .WithUseEmbeddedWebView(false)
//                            .ExecuteAsync();

//                _logger.LogTxt(methodName, $"Finish aquiring new Access Token from AAD");
//                return result.AccessToken;
//            }
//            catch (MsalServiceException ex)
//            {
//                _logger.LogTxt(methodName, $"FAILED aquiring new Access Token from AAD");
//                _logger.LogTxt(methodName, ex.Message);
//                _logger.LogTxt(methodName, $"{ex.StackTrace}");
//                throw;
//            }
//        }

//        internal async Task<string> SpoInteractiveNoTenatIdAsync(string siteUrl)
//        {
//            _appInfo.IsCancelled();
//            _logger.AddLogToTxt($"{GetType().Name}.SpoInteractiveNoTenatIdAsync - Start getting Access Token for SPO API as Interactive for '{siteUrl}'");

//            string defaultPermissions = siteUrl + "/.default";
//            string[] scopes = new string[] { defaultPermissions };

//            Uri newAuthority = new(siteUrl);

//            _logger.AddLogToTxt("Building App");
//            var app = PublicClientApplicationBuilder.Create(_clientId)
//                                                    .WithAuthority(newAuthority.Authority)
//                                                    .WithRedirectUri(redirectUri)
//                                                    .Build();

//            if (_cachingToken)
//            {
//                _logger.AddLogToTxt("Adding UserTokenCache");

//                var cacheHelper = await TokenCacheHelper.GetCache();
//                cacheHelper.RegisterCache(app.UserTokenCache);
//            }

//            AuthenticationResult result;
//            try
//            {
//                _logger.AddLogToTxt("Getting Access Token from Cache");

//                var accounts = await app.GetAccountsAsync();
//                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
//                            .ExecuteAsync();

//                _logger.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}' COMPLETED");

//                return result.AccessToken;
//            }
//            catch (MsalUiRequiredException ex)
//            {
//                _logger.AddLogToTxt("Getting Access Token from Cache failed");
//                _logger.AddLogToTxt(ex.Message);
//                _logger.AddLogToTxt($"{ex.StackTrace}");
//                _logger.AddLogToTxt("Getting new Access Token from AAD");

//                result = await app.AcquireTokenInteractive(scopes)
//                            .WithUseEmbeddedWebView(false)
//                            .ExecuteAsync();

//                _logger.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}' COMPLETED");

//                return result.AccessToken;
//            }
//            catch (MsalServiceException ex)
//            {
//                _logger.AddLogToTxt($"Getting new Access Token from AAD failed");
//                _logger.AddLogToTxt(ex.Message);
//                throw;
//            }
//        }

//        public async Task<string> SPO_Interactive_Users(Action<string> addRecord, string siteUrl)
//        {
//            addRecord("Start getting Access Token for SPO API as Interactive");

//            string permissions = siteUrl + "/AllSites.FullControl";
//            string[] scopes = new string[] { permissions };

//            var app = PublicClientApplicationBuilder.Create(_clientId)
//                                                    .WithAuthority(_authority)
//                                                    .WithRedirectUri(redirectUri)
//                                                    .Build();

//            var cacheHelper = await TokenCacheHelper.GetCache();
//            cacheHelper.RegisterCache(app.UserTokenCache);

//            AuthenticationResult result;
//            try
//            {
//                addRecord("Getting Access Token Try");

//                var accounts = await app.GetAccountsAsync();
//                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
//                            .ExecuteAsync();
//            }
//            catch (MsalUiRequiredException)
//            {
//                addRecord("Getting Access Token Catch");

//                result = await app.AcquireTokenInteractive(scopes)
//                            .WithUseEmbeddedWebView(false)
//                            .ExecuteAsync();
//            }
//            return result.AccessToken;
//        }

//    }
//}
