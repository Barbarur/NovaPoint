using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Solutions.Reports;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveUserSingleSiteAllAuto
    {
        // Baic parameters required for all reports
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;
        // Required parameters for the current report
        private readonly string _adminUPN;
        private readonly string _deleteUserUpn;
        // Optional parameters for the current report related to Sites
        private readonly bool _removeAdmin;
        private readonly bool _includeShareSite;
        private readonly bool _includePersonalSite;
        private readonly bool _groupIdDefined;

        public RemoveUserSingleSiteAllAuto(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, RemoveUserSingleSiteAllAutoParameters parameters)
        {
            // Baic parameters required for all reports
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
            // Required parameters for the current report
            _adminUPN = parameters.AdminUPN;
            _deleteUserUpn = parameters.DeleteUserUPN;
            // Optional parameters for the current report related to Sites
            _removeAdmin = parameters.RemoveAdmin;
            _includeShareSite = parameters.IncludeShareSite;
            _includePersonalSite = parameters.IncludePersonalSite;
            _groupIdDefined = parameters.GroupIdDefined;
        }
        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrEmpty(_adminUPN) || String.IsNullOrWhiteSpace(_adminUPN))
                {
                    string message = $"FORM INCOMPLETED: You need to add at least the SharePoint Amdin account to run the script";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (!_includeShareSite && _groupIdDefined)
                {
                    string message = $"FORM INCOMPLETED: If you want to get GroupIdDefined Sites, you need to include ShareSites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (_removeAdmin && (String.IsNullOrEmpty(_adminUPN) || String.IsNullOrWhiteSpace(_adminUPN)))
                {
                    string message = "FORM INCOMPLETED: SiteAdminUPN cannot be empty if you need to remove the user as User Admin as Site Collection Administraitor";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (_groupIdDefined && _includePersonalSite)
                {
                    string message = $"FORM CONTRACDICTION: If you want to get OneDrive sites, you cannot limit the filter to Group ID Defined sites";
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
            string rootPersonalSiteAccessToken = _includePersonalSite ? await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl) : "";
            string rootShareSiteAccessToken = _includeShareSite ? await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl) : "";

            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollections = new GetSiteCollection(_logHelper, adminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, _includePersonalSite, _groupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!_includePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!_includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }
            
            double counter = 0;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {

                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };

                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                counter++;
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Site Collection '{oSiteCollection.Title}'");

                string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;
                
                try
                {
                    
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                    
                    new SetSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Add(_adminUPN, oSiteCollection.Url);

                }
                catch (Exception ex)
                {
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); }; 
                    
                    ManageCatchedError(oSiteCollection, $"Error while adding Site Collection Admin: {ex.Message}", ex);
                    continue;
                
                }

                try
                {
                    
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };

                    RemoveSiteUser(currentSiteAccessToken, oSiteCollection.Url);
                    AddSiteRecordToCSV(oSiteCollection, "User Removed from Site correctly");

                }
                catch (Exception ex)
                {
                    ManageCatchedError(oSiteCollection, $"Error while removing user: {ex.Message}", ex);
                }

                // TO ADD SUBSITES IN THE FUTURE

                if (_removeAdmin)
                {
                    
                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                    
                    try
                    {
                        new RemoveSiteCollectionAdmin(_logHelper, currentSiteAccessToken, _appInfo._domain).Csom(_adminUPN, oSiteCollection.Url);
                    }
                    catch (Exception ex)
                    {
                        ManageCatchedError(oSiteCollection, $"Error while removing Site Collection Admin: {ex.Message}", ex);
                    }
                }
            }
            _logHelper.ScriptFinishSuccessfulNotice();
        }


        private void RemoveSiteUser(string accessToken, string siteUrl)
        {
            User? user = new GetUser(_logHelper, accessToken).CsomSingle(siteUrl, _deleteUserUpn);

            if (user != null)
            {
                
                if (user.IsSiteAdmin) { new RemoveSiteCollectionAdmin(_logHelper, accessToken, _appInfo._domain).Csom(siteUrl, _deleteUserUpn); }
            
                new RemoveUser(_logHelper, accessToken).Csom(siteUrl, _deleteUserUpn);
            
            }
        }

        private void ManageCatchedError(SiteProperties site, string message, Exception ex)
        {
            AddSiteRecordToCSV(site, message);
            _logHelper.AddLogToUI(message);
            _logHelper.AddLogToTxt($"Exception Message: {ex.Message}");
            _logHelper.AddLogToTxt($"Exception Trace: {ex.StackTrace}");
        }

        private void AddSiteRecordToCSV(SiteProperties site, string remarks)
        {
            dynamic recordList = new ExpandoObject();
            recordList.Title = site.Title;
            recordList.SiteUrl = site.Url;
            recordList.ID = site.GroupId;

            recordList.Remarks = remarks;

            _logHelper.AddRecordToCSV(recordList);
        }

    }


    public class RemoveUserSingleSiteAllAutoParameters
    {
        // Required parameters for the current report
        internal string AdminUPN;
        internal string DeleteUserUPN;
        // Optional parameters for the current report related to Sites
        public bool RemoveAdmin { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool GroupIdDefined { get; set; } = false;

        public RemoveUserSingleSiteAllAutoParameters(string adminUPN, string deleteUserUpn)
        {
            AdminUPN = adminUPN;
            DeleteUserUPN = deleteUserUpn;
        }
    }
}
