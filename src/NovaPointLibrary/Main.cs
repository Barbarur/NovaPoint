using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NovaPointLibrary
{
    public class Main
    {
        internal string Domain
        {
            init
            {
                _domain = value;
                _adminUrl = "https://" + value + "-admin.sharepoint.com";
                _rootPersonalUrl = "https://" + value + "-my.sharepoint.com";
                _rootSharedUrl = "https://" + value + ".sharepoint.com";
            }
        }
        internal string _domain { get; set; } = string.Empty;
        internal string _adminUrl { get; set; } = string.Empty;
        internal string _rootPersonalUrl { get; set; } = string.Empty;
        internal string _rootSharedUrl { get; set; } = string.Empty;


        internal string _tenantId = string.Empty;
        // Added PnPManagementShellClientId as default ID
        internal string _clientId = "31359c7f-bd7e-475c-86db-fdb8c937548e";
        internal bool _cachingToken = false;

        

        public Main(ISolution solution, AppInfo appInfo, Action<LogInfo> uiAddLog)
        {
            string methodName = $"{GetType().Name}.Main";

            Domain = appInfo.Domain;
            _tenantId = appInfo._tenantId;
            _clientId = appInfo._clientId;

            _cachingToken = appInfo._cachingToken;

            this.CancelTokenSource = appInfo.CancelTokenSource;
            this.CancelToken = CancelTokenSource.Token;


            Uri authority = new($"https://login.microsoftonline.com/{_tenantId}");
            _app = PublicClientApplicationBuilder.Create(_clientId)
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build();

            _uiAddLog = uiAddLog;

            string solutionName = solution.GetType().Name;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = solutionName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, folderName);
            System.IO.Directory.CreateDirectory(folderPath);

            _txtPath = System.IO.Path.Combine(folderPath, folderName + "_Logs.txt");
            _csvPath = System.IO.Path.Combine(folderPath, folderName + "_Report.csv");

            AddLogToTxt(methodName, $"Solution logs can be found at: {_txtPath}");
            AddLogToTxt(methodName, $"Solution report can be found at: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(folderPath));

            SolutionProperties(solution.Parameters);

            SW.Start();

            AddLogToUI(methodName, $"Solution has started, please wait to the end");

        }

        private void SolutionProperties(ISolutionParameters parameters)
        {
            string methodName = $"{GetType().Name}.SolutionProperties";
            AddLogToTxt(methodName, $"Start adding Solution properties");


            Type solutiontype = parameters.GetType();
            PropertyInfo[] properties = solutiontype.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in properties)
            {
                AddLogToTxt(methodName, $"{propertyInfo.Name}: {propertyInfo.GetValue(parameters)}");
            }

            AddLogToTxt(methodName, $"Finish adding Solution properties");
        }


        public CancellationTokenSource CancelTokenSource { get; init; }
        public CancellationToken CancelToken { get; init; }

        private IPublicClientApplication _app;

        public void IsCancelled()
        {
            if (CancelToken.IsCancellationRequested) { CancelToken.ThrowIfCancellationRequested(); }
        }

        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }

        private readonly Action<LogInfo> _uiAddLog;

        private readonly string _txtPath;
        internal readonly string _csvPath;

        private readonly Stopwatch SW = new();

        internal void AddLogToTxt(string classMethod, string log)
        {
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}");
        }

        internal void AddLogToUI(string classMethod, string log)
        {
            AddLogToTxt(classMethod, log);

            LogInfo logInfo = new(log);
            _uiAddLog(logInfo);
        }

        internal void AddProgressToUI(double progress)
        {
            AddLogToTxt($"{GetType().Name}.AddProgressToUI", $"Progress {progress}%");
            string pendingTime = $"Pending Time: Calculating...";

            if (progress > 1)
            {
                TimeSpan ts = TimeSpan.FromMilliseconds((SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds));
                pendingTime = $"Pending Time: {ts.Hours}h:{ts.Minutes}m:{ts.Seconds}s";
            }

            LogInfo logInfo = new(progress, pendingTime);
            _uiAddLog(logInfo);
        }

        internal void AddRecordToCSV(dynamic o)
        {
            string methodName = $"{GetType().Name}.AddRecordToCSV";
            AddLogToTxt(methodName, $"Adding Record to csv report");

            StringBuilder sb = new();
            using StreamWriter csv = new(new FileStream(_csvPath, FileMode.Append, FileAccess.Write));
            {
                var csvFileLenth = new System.IO.FileInfo(_csvPath).Length;
                if (csvFileLenth == 0)
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?redirectedfrom=MSDN&view=net-7.0#enumerating-and-deleting-members
                    foreach (var property in (IDictionary<String, Object>)o)
                    {
                        sb.Append($"{property.Key},");
                    }

                    csv.WriteLine(sb.ToString());
                    sb.Clear();
                }

                foreach (var property in (IDictionary<String, Object>)o)
                {
                    sb.Append($"{property.Value},");
                }

                csv.WriteLine(sb.ToString());
            }
        }

        internal void ScriptFinish()
        {
            ScriptFinishNotice();
            AddLogToUI($"{GetType().Name}.ScriptFinish", $"COMPLETED: Solution has finished correctly!");
        }

        internal void ScriptFinish(Exception ex)
        {
            ScriptFinishNotice();
            AddLogToUI($"{GetType().Name}.ScriptFinish", ex.Message);
            AddLogToTxt($"{GetType().Name}.ScriptFinish", $"{ex.StackTrace}");
        }

        private void ScriptFinishNotice()
        {
            SW.Stop();
            AddProgressToUI(100);
        }

        internal void ReportError(string type, string URL, Exception ex)
        {
            AddLogToUI($"{GetType().Name}.ScriptFinish", $"Error processing {type} '{URL}'");
            AddLogToTxt($"{GetType().Name}.ScriptFinish", $"Exception: {ex.Message}");
            AddLogToTxt($"{GetType().Name}.ScriptFinish", $"Trace: {ex.StackTrace}");
        }



        internal async Task<ClientContext> GetContext(string siteUrl)
        {
            string accessToken = await GetSPOAccessToken(siteUrl);

            var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return clientContext;
        }


        private AuthenticationResult? _adminAuthenticationResult = null;
        private AuthenticationResult? _rootPersonalAuthenticationResult = null;
        private AuthenticationResult? _rootSharedAuthenticationResult = null;

        private async Task<string> GetSPOAccessToken(string siteUrl)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetSPOAccessToken";

            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string defaultPermissions = rootUrl + "/.default";
            string[] scopes = new string[] { defaultPermissions };

            AddLogToTxt(methodName, $"Start getting Access Token for root site {rootUrl}");

            AuthenticationResult? result = null;
            if (rootUrl.Equals(_adminUrl, StringComparison.OrdinalIgnoreCase))
            {
                _adminAuthenticationResult = await GetAccessTokenFromMemory(_adminAuthenticationResult, scopes);
                result = _adminAuthenticationResult;
            }
            else if (rootUrl.Equals(_rootSharedUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootSharedAuthenticationResult = await GetAccessTokenFromMemory(_rootSharedAuthenticationResult, scopes);
                result = _rootSharedAuthenticationResult;
            }
            else if (rootUrl.Equals(_rootPersonalUrl, StringComparison.OrdinalIgnoreCase))
            {
                _rootPersonalAuthenticationResult = await GetAccessTokenFromMemory(_rootPersonalAuthenticationResult, scopes);
                result = _rootPersonalAuthenticationResult;
            }

            if (result != null)
            {
                AddLogToTxt(methodName, $"Access Token expiration time: {result.ExpiresOn.ToString()}");
                return result.AccessToken;
            }
            else
            {
                throw new Exception("Access Token could not be aquired");
            }
        }

        private async Task<AuthenticationResult> GetAccessTokenFromMemory(AuthenticationResult? cachedResult, string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetAccessTokenFromMemory";
            AddLogToTxt(methodName, $"Start getting Access Token from memory");


            AuthenticationResult? result = null;

            if (cachedResult != null)
            {
                var timeNow = DateTime.UtcNow;
                var difference = cachedResult.ExpiresOn.Subtract(timeNow);
                //AddLogToTxt(methodName, $"ExpiresOn '{cachedResult.ExpiresOn}'");
                //AddLogToTxt(methodName, $"Time to expire '{difference.TotalMinutes}' minutes");

                if (difference.TotalMinutes > 10)
                {
                    AddLogToTxt(methodName, $"Got access token from memory");
                    result = cachedResult;
                }
            }

            result ??= await GetAccessToken(scopes);

            return result;
        }


        private async Task<AuthenticationResult> GetAccessToken(string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetAccessToken";
            AddLogToTxt(methodName, $"Start getting Access Token");

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
                AddLogToTxt(methodName, $"Finish getting Access Token");
                return await aquireToken;
            }
        }

        private async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
            this.IsCancelled();
            string methodName = $"{GetType().Name}.GetTokenDinamicaly";
            AddLogToTxt(methodName, $"Start aquiring Access Token");

            if (_cachingToken)
            {
                AddLogToTxt(methodName, "Adding cached access token");

                var cacheHelper = await TokenCacheHelper.GetCache();
                cacheHelper.RegisterCache(_app.UserTokenCache);
            }

            AuthenticationResult result;
            try
            {
                AddLogToTxt(methodName, $"Start aquiring Access Token from Cache");

                var accounts = await _app.GetAccountsAsync();
                result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                AddLogToTxt(methodName, $"Finish aquiring Access Token from Cache");
            }
            catch (MsalUiRequiredException ex)
            {
                if (this.CancelToken.IsCancellationRequested) { this.CancelToken.ThrowIfCancellationRequested(); };
                AddLogToTxt(methodName, ex.Message);
                AddLogToTxt(methodName, $"{ex.StackTrace}");
                AddLogToTxt(methodName, $"Start aquiring new Access Token from AAD");

                result = await _app.AcquireTokenInteractive(scopes)
                                  .WithUseEmbeddedWebView(false)
                                  .ExecuteAsync();

                AddLogToTxt(methodName, $"Finish aquiring new Access Token from AAD");
            }
            catch (MsalServiceException ex)
            {
                AddLogToTxt(methodName, $"FAILED aquiring new Access Token from AAD");
                AddLogToTxt(methodName, ex.Message);
                AddLogToTxt(methodName, $"{ex.StackTrace}");
                throw;
            }

            //AddLogToTxt(methodName, $"Expires On: {result.ExpiresOn}");
            //AddLogToTxt(methodName, $"Token: {result.AccessToken}");

            return result;
        }
    }
}
