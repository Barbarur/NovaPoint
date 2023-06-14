using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using User = Microsoft.SharePoint.Client.User;

namespace NovaPointLibrary.Solutions.QuickFix
{
    public class IdMismatchTrouble
    {
        public static string _solutionName = "Report of all Permissions in a Site";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;
        
        private readonly string _userUpn;
        private readonly string _siteUrl;
        private readonly string _adminUpn;
        
        private readonly bool _preventAllSites;
        private readonly bool _removeAdmin;


        public IdMismatchTrouble(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo,
                                 IdMismatchTroubleParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
            
            _userUpn = parameters.UserUpn;
            _siteUrl = parameters.SiteUrl;
            _adminUpn = parameters.AdminUpn;

            _preventAllSites = parameters.PreventAllSites;
            _removeAdmin = parameters.RemoveAdmin;
        }


        public async Task RunAsync()
        {
            try
            {
                if ( string.IsNullOrWhiteSpace(_userUpn) || string.IsNullOrWhiteSpace(_siteUrl) || string.IsNullOrWhiteSpace(_adminUpn) )
                {
                    string message = $"FORM INCOMPLETED: Please fill up the form";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }


        private async Task RunScriptAsync()
        {
            _logHelper.ScriptStartNotice();

            string adminAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);

            string rootUrl = _siteUrl.Substring(0, _siteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(rootUrl);

            SingleSiteAsync(adminAccessToken, _siteUrl, rootSiteAccessToken, "abcdefghijk");

            if (_preventAllSites) 
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                new RegisterUser(_logHelper, rootSiteAccessToken).Csom(_siteUrl, _userUpn);

                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                User? user = new GetUser(_logHelper, rootSiteAccessToken).CsomSingle(_siteUrl, _userUpn);
                if (user != null) { throw new Exception("User couldn't be found to obtain connect user ID"); }

                UserIdInfo userIdInfo = user.UserId;
                string userCorrectId = userIdInfo.NameId;

                await AllSitesAsync(adminAccessToken, userCorrectId); 
            }

            _logHelper.ScriptFinishSuccessfulNotice();
        }

        private void SingleSiteAsync(string adminAccessToken, string siteUrl, string siteAccessToken, string correctUserID)
        {
            try
            {
                _logHelper.AddLogToUI($"Processing site: {siteUrl}");

                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                new SetSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Add(_adminUpn, siteUrl);

                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                User? user = new GetUser(_logHelper, siteAccessToken).CsomSingle(siteUrl, _userUpn);

                if (user == null) { return; }

                string siteUserID = ((UserIdInfo)user.UserId).NameId;
                if (siteUserID == correctUserID)
                {
                    if (user.IsSiteAdmin)
                    {
                        if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                        new RemoveSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Csom(siteUrl, user.UserPrincipalName);
                    }

                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                    new RemoveUser(_logHelper, siteAccessToken).Csom(siteUrl, user.UserPrincipalName);

                    string remarks = "User with incorrect ID found on Site and Removed";

                    dynamic recordSite = new ExpandoObject();
                    recordSite.SiteUrl = siteUrl;
                    recordSite.Remarks = remarks;
                    _logHelper.AddRecordToCSV(recordSite);

                    _logHelper.AddLogToUI(remarks);
                }

                string urlOwnerODBCheckUp = _userUpn.Replace("@", "_").Replace(".", "_");
                if (_siteUrl.Contains(urlOwnerODBCheckUp) && _siteUrl.Contains("-my.sharepoint.com"))
                {
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                    new SetSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Add(user.UserPrincipalName, siteUrl);
                }

                if (_removeAdmin)
                {
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                    new RemoveSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Csom(siteUrl, _adminUpn);
                }

            }
            catch(Exception ex)
            {
                string remarks = $"Error: {ex.Message}";

                dynamic recordError = new ExpandoObject();
                recordError.SiteUrl = siteUrl;
                recordError.Remarks = remarks;
                _logHelper.AddRecordToCSV(recordError);

                _logHelper.AddLogToUI(remarks);
            }

        }


        private async Task AllSitesAsync(string adminAccessToken, string correctUserID)
        {
            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
            string rootPersonalSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl);
            string rootShareSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl);

            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollections = new GetSiteCollection(_logHelper, adminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, true);
            double counter = 0;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };

                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                counter++;
                _logHelper.AddProgressToUI(progress);

                string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;

                SingleSiteAsync(adminAccessToken, oSiteCollection.Url, currentSiteAccessToken, correctUserID);

            }

        }

    }

    public class IdMismatchTroubleParameters
    {
        internal readonly string UserUpn;
        internal readonly string SiteUrl;
        internal readonly string AdminUpn;
        public bool RemoveAdmin { get; set; } = false;
        public bool PreventAllSites { get; set; } = false;

        public IdMismatchTroubleParameters(string userUpn, string siteUrl, string adminUpn)
        {
            UserUpn = userUpn;
            SiteUrl = siteUrl;
            AdminUpn = adminUpn;
        }
    }
}
