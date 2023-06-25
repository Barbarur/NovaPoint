using CamlBuilder;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.IdentityModel.Logging;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Authentication
{
    internal class GetAccessToken
    {
        private Solutions.LogHelper _logHelper;
        private AppInfo _appInfo;

        private readonly string _clientId = string.Empty;
        private readonly  Uri? _authority;
        private readonly bool _cachingToken;
        private readonly string redirectUri = "http://localhost";

        internal GetAccessToken(Solutions.LogHelper logHelper, AppInfo appInfo)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;

            _authority = new Uri($"https://login.microsoftonline.com/{appInfo._tenantId}");
            _clientId = appInfo._clientId;
            _cachingToken = appInfo._cachingToken;

        }

        internal async Task<string> GraphInteractiveAsync()
        {
            //_appInfo.IsCancelled();
            //_logHelper.AddLogToTxt($"{GetType().Name}.GraphInteractiveAsync - Start getting Graph Access Token");

            //return await GraphTest();

            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphInteractiveAsync";
            _logHelper.AddLogToTxt(methodName, $"Start getting Graph Access Token");

            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
            var aquireGraphToken = GraphInteractiveAquireTokenAsync();

            TaskCompletionSource taskCompletionSource = new();

            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(aquireGraphToken, taskCompletionSource.Task);

            if (completedTask != aquireGraphToken || _appInfo.CancelToken.IsCancellationRequested)
            {
                _appInfo.CancelToken.ThrowIfCancellationRequested();
                return null;
            }
            else
            {
                return await aquireGraphToken;
            }

        }

        private async Task<string> GraphInteractiveAquireTokenAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphInteractiveAquireTokenAsync";
            _logHelper.AddLogToTxt(methodName, $"Start aquiring Graph Access Token");

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            if (_cachingToken)
            {
                _logHelper.AddLogToTxt(methodName, $"Adding UserTokenCache");

                MsalCacheHelper cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(app.UserTokenCache);
            }

            AuthenticationResult result;
            try
            {
                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(false)
                            .ExecuteAsync();
            }

            _logHelper.AddLogToTxt(methodName, $"Finish aquiring Graph Access Token");
            return result.AccessToken;
        }

        // TO BE DEPRECATED
        internal async Task<string> Graph_Interactive()
        {
            string[] scopes = new string[] { "https://graph.microsoft.com/Sites.FullControl.All" };

            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            MsalCacheHelper cacheHelper = await TokenCacheHelper.GetCache();
            cacheHelper.RegisterCache(app.UserTokenCache);

            AuthenticationResult result;
            try
            {
                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(false)
                            .ExecuteAsync();
            }
            return result.AccessToken;

        }

        internal async Task<string> SpoInteractiveAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.SpoInteractiveAsync";
            _logHelper.AddLogToTxt(methodName, $"Start getting SPO Access Token for '{siteUrl}'");

            string defaultPermissions = siteUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            _logHelper.AddLogToTxt(methodName, "Building App");
            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            if (_cachingToken)
            {
                _logHelper.AddLogToTxt(methodName, "Adding UserTokenCache");

                var cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(app.UserTokenCache);
            }

            // Reference: https://johnthiriet.com/cancel-asynchronous-operation-in-csharp/
            var aquireToken = SpoInteractiveAcquireTokenAsync(app, scopes);

            TaskCompletionSource taskCompletionSource = new();

            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(aquireToken, taskCompletionSource.Task);

            if (completedTask != aquireToken || _appInfo.CancelToken.IsCancellationRequested )
            {
                _appInfo.CancelToken.ThrowIfCancellationRequested();
                return null;
            }
            else
            {
                _logHelper.AddLogToTxt(methodName, $"Finish getting SPO Access Token for '{siteUrl}'");
                return await aquireToken;
            }

        }

        private async Task<string> SpoInteractiveAcquireTokenAsync(IPublicClientApplication app, string[] scopes)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.SpoInteractiveAcquireTokenAsync";
            _logHelper.AddLogToTxt(methodName, $"Start aquiring SPO Access Token");

            AuthenticationResult result;
            try
            {
                _logHelper.AddLogToTxt(methodName, $"Start aquiring Access Token from Cache");

                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                _logHelper.AddLogToTxt(methodName, $"Finish aquiring Access Token from Cache");

                return result.AccessToken;
            }
            catch (MsalUiRequiredException ex)
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                _logHelper.AddLogToTxt(methodName, ex.Message);
                _logHelper.AddLogToTxt(methodName, $"{ex.StackTrace}");
                _logHelper.AddLogToTxt(methodName, $"Start aquiring new Access Token from AAD");

                result = await app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(false)
                            .ExecuteAsync();

                _logHelper.AddLogToTxt(methodName, $"Finish aquiring new Access Token from AAD");
                return result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                _logHelper.AddLogToTxt(methodName, $"FAILED aquiring new Access Token from AAD");
                _logHelper.AddLogToTxt(methodName, ex.Message);
                _logHelper.AddLogToTxt(methodName, $"{ex.StackTrace}");
                throw;
            }
        }

        internal async Task<string> SpoInteractiveNoTenatIdAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"{GetType().Name}.SpoInteractiveNoTenatIdAsync - Start getting Access Token for SPO API as Interactive for '{siteUrl}'");

            string defaultPermissions = siteUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            Uri newAuthority = new(siteUrl);

            _logHelper.AddLogToTxt("Building App");
            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(newAuthority.Authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            if (_cachingToken)
            {
                _logHelper.AddLogToTxt("Adding UserTokenCache");

                var cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(app.UserTokenCache);
            }

            AuthenticationResult result;
            try
            {
                _logHelper.AddLogToTxt("Getting Access Token from Cache");

                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                _logHelper.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}' COMPLETED");

                return result.AccessToken;
            }
            catch (MsalUiRequiredException ex)
            {
                _logHelper.AddLogToTxt("Getting Access Token from Cache failed");
                _logHelper.AddLogToTxt(ex.Message);
                _logHelper.AddLogToTxt($"{ex.StackTrace}");
                _logHelper.AddLogToTxt("Getting new Access Token from AAD");

                result = await app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(false)
                            .ExecuteAsync();

                _logHelper.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}' COMPLETED");

                return result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                _logHelper.AddLogToTxt($"Getting new Access Token from AAD failed");
                _logHelper.AddLogToTxt(ex.Message);
                throw;
            }
        }

        public async Task<string> SPO_Interactive_Users(Action<string> addRecord, string siteUrl)
        {
            addRecord("Start getting Access Token for SPO API as Interactive");

            string permissions = siteUrl + "/AllSites.FullControl";
            string[] scopes = new string[] { permissions };

            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            var cacheHelper = await TokenCacheHelper.GetCache();
            cacheHelper.RegisterCache(app.UserTokenCache);

            AuthenticationResult result;
            try
            {
                addRecord("Getting Access Token Try");

                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                addRecord("Getting Access Token Catch");

                result = await app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(false)
                            .ExecuteAsync();
            }
            return result.AccessToken;
        }

    }
}
