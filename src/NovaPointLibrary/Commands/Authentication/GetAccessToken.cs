using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
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
        private AppInfo AppInfo
        {
            init
            {
                _authority = new Uri($"https://login.microsoftonline.com/{value._tenantId}");
                _clientId = value._clientId;
                _cachingToken = value._cachingToken;
            }
        }
        private readonly string _clientId = string.Empty;
        private readonly  Uri? _authority;
        private readonly bool _cachingToken;
        private readonly string redirectUri = "http://localhost";

        private LogHelper _logHelper;

        internal GetAccessToken(LogHelper logHelper, AppInfo appInfo)
        {
            _logHelper = logHelper;
            AppInfo = appInfo;

        }

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
            _logHelper = new(_logHelper, $"{GetType().Name}.SpoInteractiveAsync");

            _logHelper.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}'");

            string defaultPermissions = siteUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            _logHelper.AddLogToTxt("Building App");
            var app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(_authority)
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

        internal async Task<string> SpoInteractiveNoTenatIdAsync(string siteUrl)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.SpoInteractiveNoTenatIdAsync");

            _logHelper.AddLogToTxt($"Getting Access Token for SPO API as Interactive for '{siteUrl}'");

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
