using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Solutions.Reports;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteAllReport
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

        private readonly bool IncludeSubsites = false;

        public SiteAllReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, SiteAllReportParameters parameters)
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

            IncludeSubsites = parameters.IncludeSubsites;
        }

        public async Task RunAsync()
        {
            try
            {
                if ( IncludeAdmins && String.IsNullOrWhiteSpace(AdminUPN) )
                {
                    string message = "FORM INCOMPLETED: Admin UPN cannot be empty if you want to include Site Collection Administrators";
                    Exception ex = new(message);
                    throw ex;
                }
                else if ( !IncludeShareSite && GroupIdDefined )
                {
                    string message = $"FORM INCOMPLETED: If you want to get GroupIdDefined Sites, you want to include ShareSites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if ( IncludeSubsites && String.IsNullOrWhiteSpace(AdminUPN))
                {
                    string message = "FORM INCOMPLETED: Admin UPN cannot be empty if you waint to include Subsites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if ( RemoveAdmin && String.IsNullOrWhiteSpace(AdminUPN) )
                {
                    string message = "FORM INCOMPLETED: Admin UPN cannot be empty if you need to remove the user as User Admin as Site Collection Administraitor";
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
            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

            _logHelper.ScriptStartNotice();

            string? adminAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);
            string? rootPersonalSiteAccessToken = String.Empty;
            string? rootShareSiteAccessToken = String.Empty;
            if(IncludeAdmins || IncludeSubsites)
            {
            
                if (IncludePersonalSite) { rootPersonalSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl); }
                if (IncludeShareSite) { rootShareSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl); }

            }

            List<SiteProperties> collSiteCollections = new GetSiteCollection(_logHelper, adminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, IncludePersonalSite, GroupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!IncludePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!IncludeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            double counter = 0;
            double counterStep = 1 / collSiteCollections.Count;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                string? currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;

                counter++;
                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Site Collestion '{oSiteCollection.Title}'");


                try
                {

                    if (IncludeAdmins || IncludeSubsites)
                    {

                        new SetSiteCollectionAdmin(_logHelper, adminAccessToken, _appInfo._domain).Add(AdminUPN, oSiteCollection.Url);

                    }

                    string admins = string.Empty;
                    if (IncludeAdmins)
                    {

                        admins = GetAdmins(_logHelper, currentSiteAccessToken, oSiteCollection.Url);

                    }

                    AddSiteRecordToCSV(true, siteCollection: oSiteCollection, admins: admins);

                    if (IncludeSubsites)
                    {
                        var collSubsites = new GetSubsite(_logHelper, _appInfo, currentSiteAccessToken).CsomAllSubsitesBasicExpressions(oSiteCollection.Url);

                        foreach (var oSubsite in collSubsites)
                        {
                            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                            progress = Math.Round(progress + counterStep / (collSubsites.Count + 1), 2);
                            _logHelper.AddProgressToUI(progress);
                            _logHelper.AddLogToUI($"Processing Subsite '{oSubsite.Title}'");

                            AddSiteRecordToCSV(false, subsiteWeb: oSubsite);

                        }
                    }

                    if (IncludeAdmins && RemoveAdmin)
                    {

                        if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };
                        new RemoveSiteCollectionAdmin(_logHelper, currentSiteAccessToken, _appInfo._domain).Csom(AdminUPN, oSiteCollection.Url);

                    }
                }
                catch (Exception ex)
                {

                    _logHelper.AddLogToUI($"Error processing Site Collestion '{oSiteCollection.Url}'");
                    _logHelper.AddLogToTxt($"Exception: {ex.Message}");
                    _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");
                    AddSiteRecordToCSV(true, siteCollection: oSiteCollection, remarks: ex.Message);

                }
            }

            _logHelper.ScriptFinishSuccessfulNotice();

        }

        private string GetAdmins(LogHelper logHelper, string accessToken, string siteUrl)
        {
            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

            StringBuilder sb = new();

            var collSiteCollAdmins = new GetSiteCollectionAdmin(logHelper, accessToken).Csom(siteUrl);
            foreach (Microsoft.SharePoint.Client.User oAdmin in collSiteCollAdmins)
            {
                sb.Append($"{oAdmin.Email} ");
            }
            return sb.ToString();
        }

        private void AddSiteRecordToCSV(bool isSiteCollection, SiteProperties? siteCollection = null, Web? subsiteWeb = null, string admins = "", string remarks = "")
        {

            dynamic record = new ExpandoObject();
            record.Title = isSiteCollection ? siteCollection?.Title : subsiteWeb?.Title;
            record.SiteUrl = isSiteCollection ? siteCollection?.Url : subsiteWeb?.Url;
            record.GroupId = isSiteCollection ? siteCollection?.GroupId.ToString() : string.Empty;
            record.Tempalte = isSiteCollection ? siteCollection?.Template : subsiteWeb.WebTemplate;


            record.StorageQuotaGB = isSiteCollection ? string.Format("{0:N2}", Math.Round(siteCollection.StorageMaximumLevel / Math.Pow(1024, 3), 2)) : string.Empty;
            record.StorageUsedGB = isSiteCollection ? string.Format("{0:N2}", Math.Round(siteCollection.StorageUsage / Math.Pow(1024, 3), 2)) : string.Empty; // ADD CONSUMPTION FOR SUBSITES
            record.storageWarningLevelGB = isSiteCollection ? string.Format("{0:N2}", Math.Round(siteCollection.StorageWarningLevel / Math.Pow(1024, 3), 2)) : string.Empty;



            record.IsHubSite = isSiteCollection ? siteCollection?.IsHubSite.ToString() : string.Empty;
            record.LastContentModifiedDate = isSiteCollection ? siteCollection?.LastContentModifiedDate.ToString() : subsiteWeb?.LastItemModifiedDate.ToString();
            record.LockState = isSiteCollection ? siteCollection?.LockState.ToString() : string.Empty;

            record.SiteCollectionAdminstrators = isSiteCollection ? admins : string.Empty;

            record.Remarks = remarks;

            _logHelper.AddRecordToCSV(record);
        }
    }
    public class SiteAllReportParameters
    {
        public bool IncludeAdmins { get; set; } = false;
        public string AdminUPN { get; set; } = "";
        public bool RemoveAdmin { get; set; } = false;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;
        public bool IncludeSubsites { get; set; } = false;
    }
}
