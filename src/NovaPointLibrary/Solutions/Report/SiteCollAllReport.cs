using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Discovery;
using Newtonsoft.Json;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.Site;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Solutions.Reports
{
    public class SiteCollAllReport
    {
        // Baic parameters required for all reports
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;
        
        // Optional parameters for current report
        private readonly bool IncludeAdmins = false;
        private readonly string AdminUPN = "";

        private readonly bool RemoveAdmin = false;
        
        private readonly bool IncludePersonalSite = false;
        private readonly bool IncludeShareSite = true;
        private readonly bool GroupIdDefined = false;

        public SiteCollAllReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, SiteCollAllReportParameters parameters)
        {
            // Baic parameters required for all reports
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
            
            // Optional parameters for current report
            IncludeAdmins = parameters.IncludeAdmins;
            AdminUPN = parameters.AdminUPN;
            
            RemoveAdmin = parameters.RemoveAdmin;
            
            IncludeShareSite = parameters.IncludeShareSite;
            IncludePersonalSite = parameters.IncludePersonalSite;
            GroupIdDefined = parameters.GroupIdDefined;
        }

        public async Task RunAsync()
        {
            try
            {
                if (IncludeAdmins && ( String.IsNullOrEmpty(AdminUPN) || String.IsNullOrWhiteSpace(AdminUPN) ) )
                {
                    string message = "FORM INCOMPLETED: SiteAdminUPN cannot be empty if you need to obtain the Site Collection Administrators";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (!IncludeShareSite && GroupIdDefined)
                {
                    string message = $"FORM INCOMPLETED: If you want to get GroupIdDefined Sites, you need to include ShareSites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (RemoveAdmin && (String.IsNullOrEmpty(AdminUPN) || String.IsNullOrWhiteSpace(AdminUPN)))
                {
                    string message = "FORM INCOMPLETED: SiteAdminUPN cannot be empty if you need to remove the user as User Admin as Site Collection Administraitor";
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
            string rootPersonalSiteAccessToken = String.Empty;
            string rootShareSiteAccessToken = String.Empty;
            if ( IncludeAdmins && IncludePersonalSite) { rootPersonalSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl); }
            if (IncludeAdmins && IncludeShareSite) { rootShareSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl); }

            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollections = new GetSiteCollection(_logHelper, adminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, IncludePersonalSite, GroupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!IncludePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!IncludeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            double counter = 0;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                if(_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };
                
                counter++;
                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Site '{oSiteCollection.Title}'");

                dynamic recordSite = new ExpandoObject();
                recordSite.Title = oSiteCollection.Title;
                recordSite.SiteUrl = oSiteCollection.Url;
                recordSite.ID = oSiteCollection.GroupId;

                float storageQuotaGB = (float)Math.Round(oSiteCollection.StorageMaximumLevel / Math.Pow(1024, 3), 2);
                float storageUsedGB = (float)Math.Round(oSiteCollection.StorageUsage / Math.Pow(1024, 3), 2);
                float storageWarningLevelGB = (float)Math.Round(oSiteCollection.StorageWarningLevel / Math.Pow(1024, 3), 2);
                recordSite.StorageQuotaGB = storageQuotaGB;
                recordSite.StorageUsedGB = storageUsedGB;
                recordSite.storageWarningLevelGB = storageWarningLevelGB;
                    
                    
                recordSite.IsHubSite = oSiteCollection.IsHubSite;
                recordSite.LastContentModifiedDate = oSiteCollection.LastContentModifiedDate;
                recordSite.LockState = oSiteCollection.LockState;

                if (IncludeAdmins)
                {
                    if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                    new SetSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Add(AdminUPN, oSiteCollection.Url);
                    
                    string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;
                    
                    try
                    {

                        recordSite.AdminUPN = GetAdmins(_logHelper, currentSiteAccessToken, oSiteCollection.Url);

                    }
                    catch (Exception ex)
                    {
                        _logHelper.AddLogToUI($"Error getting Site CollestionAdmins of: {oSiteCollection.Url}");
                        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
                        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");
                        recordSite.AdminsEmail = ex.Message;
                    }
                
                    if (RemoveAdmin)
                    {
                        if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                        try
                        {
                            new RemoveSiteCollectionAdmin(_logHelper, currentSiteAccessToken, _appInfo._domain).Csom(AdminUPN, oSiteCollection.Url);
                            recordSite.AdminRemoved = true;
                        }
                        catch
                        {
                            recordSite.AdminRemoved = false;
                        }
                    }
                }
                
                _logHelper.AddRecordToCSV(recordSite);

            }
            _logHelper.ScriptFinishSuccessfulNotice();
        }


        private static string GetAdmins(LogHelper logHelper, string accessToken, string siteUrl)
        {
            StringBuilder sb = new();

            var collSiteCollAdmins = new GetSiteCollectionAdmin(logHelper, accessToken).Csom(siteUrl);
            foreach (Microsoft.SharePoint.Client.User oAdmin in collSiteCollAdmins)
            {
                sb.Append($"{oAdmin.Email} ");
            }
            return sb.ToString();
        }
    }

    public class SiteCollAllReportParameters
    {
        public bool IncludeAdmins { get; set; } = false;
        public string AdminUPN { get; set; } = "";
        public bool RemoveAdmin { get; set; } = false;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;
    }
}
