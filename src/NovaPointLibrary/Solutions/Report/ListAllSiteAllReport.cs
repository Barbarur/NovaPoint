using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.Site;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    public class ListAllSiteAllReport
    {
        // Baic parameters required for all reports
        private readonly LogHelper _LogHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;
        // Required parameters for the current report
        private readonly string Domain;
        private readonly string AdminUrl;
        private readonly string RootShareSiteUrl;
        private readonly string RootPersonalSiteUrl;
        private readonly string SiteAdminUPN;
        // Optional parameters related to filter sites
        private readonly bool RemoveAdmin;
        private readonly bool IncludeShareSite;
        private readonly bool IncludePersonalSite;
        private readonly bool GroupIdDefined;
        // Optional parameters to filter lists
        private readonly bool IncludeSystemLists;
        private readonly bool IncludeResourceLists;

         public ListAllSiteAllReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, ListAllSiteAllReportParameters parameters)
        {
            // Baic parameters required for all reports
            _LogHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
            // Required parameters for the current report
            SiteAdminUPN = parameters.SiteAdminUPN;
            // Optional parameters related to filter sites
            RemoveAdmin = parameters.RemoveAdmin;
            IncludeShareSite = parameters.IncludeShareSite;
            IncludePersonalSite = parameters.IncludePersonalSite;
            GroupIdDefined = parameters.GroupIdDefined;
            // Optional parameters to filter lists
            IncludeSystemLists = parameters.IncludeSystemLists;
            IncludeResourceLists = parameters.IncludeResourceLists;

        }
        public async Task RunAsync()
        {
            try
            {
                if( String.IsNullOrEmpty(SiteAdminUPN) || String.IsNullOrWhiteSpace(SiteAdminUPN) )
                {
                    string message = $"FORM INCOMPLETED: You need to add at least the SharePoint Amdin account to run the script";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (!IncludeShareSite && GroupIdDefined)
                {
                    string message = $"FORM INCOMPLETED: If you want to get GroupIdDefined Sites, you need to include ShareSites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (RemoveAdmin && (String.IsNullOrEmpty(SiteAdminUPN) || String.IsNullOrWhiteSpace(SiteAdminUPN)))
                {
                    string message = "FORM INCOMPLETED: SiteAdminUPN cannot be empty if you need to remove the user as User Admin as Site Collection Administraitor";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (GroupIdDefined && IncludePersonalSite)
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
                _LogHelper.ScriptFinishErrorNotice(ex);
            }
        }


        private async Task RunScriptAsync()
        {
            _LogHelper.ScriptStartNotice();

            string adminAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(AdminUrl);
            string rootPersonalSiteAccessToken = "";
            string rootShareSiteAccessToken = "";
            if (IncludePersonalSite) { rootPersonalSiteAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl); }
            if (IncludeShareSite) { rootShareSiteAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl); }

            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollections = new GetSiteCollection(_LogHelper, adminAccessToken).CSOM_AdminAll(AdminUrl, IncludePersonalSite, GroupIdDefined);
            double counter = 0;
            List<List> collList;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                if (oSiteCollection.Title == "" || oSiteCollection.Template.Contains("Redirect")) { continue; }
                if (oSiteCollection.Template.Contains("SPSPERS") && !IncludePersonalSite) { continue; }
                if (!oSiteCollection.Template.Contains("SPSPERS") && !IncludeShareSite) { continue; }

                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                counter++;
                _LogHelper.AddProgressToUI(progress);

                try
                {
                    if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                    new SetSiteCollectionAdmin(_LogHelper, adminAccessToken, _appInfo._domain).Add(SiteAdminUPN, oSiteCollection.Url);

                    string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;

                    collList = new GetList(_LogHelper, currentSiteAccessToken).CSOM_All(oSiteCollection.Url, IncludeSystemLists, IncludeResourceLists);

                    foreach (List oList in collList)
                    {
                        if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                        AddListRecordToCSV(oSiteCollection, list: oList);

                    }
                    
                    if (RemoveAdmin)
                    {

                        try
                        {
                            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };

                            new RemoveSiteCollectionAdmin(_LogHelper, currentSiteAccessToken, _appInfo._domain).Csom(SiteAdminUPN, oSiteCollection.Url);
                        }
                        catch (Exception e)
                        {
                            AddListRecordToCSV(oSiteCollection, remarks: e.Message);
                        }
                    
                    }

                }
                catch (Exception ex)
                {
                    
                    _LogHelper.AddLogToUI($"Error processing site: {oSiteCollection.Url}");
                    _LogHelper.AddLogToTxt($"Exception: {ex.Message}");
                    _LogHelper.AddLogToTxt($"Exception: {ex.StackTrace}");

                    AddListRecordToCSV(oSiteCollection, remarks: ex.Message);
                
                }
            }

            _LogHelper.ScriptFinishSuccessfulNotice();
        
        }

        private void AddListRecordToCSV(SiteProperties site, string remarks = "", List? list = null)
        {
            dynamic recordList = new ExpandoObject();
            recordList.Title = site.Title;
            recordList.SiteUrl = site.Url;
            recordList.ID = site.GroupId;

            recordList.LibraryName = list != null ? list.Title : string.Empty;
            recordList.LibraryType = list != null ? list.BaseType.ToString() : string.Empty;

            recordList.MajorVersionLimit = list != null ? list.MajorVersionLimit.ToString() : string.Empty;
            recordList.MinorVersionLimit = list != null ? list.EnableMinorVersions.ToString() : string.Empty;
            recordList.MinorVersionsLimit = list != null ? list.MajorWithMinorVersionsLimit.ToString() : string.Empty;

            recordList.IRM_Emabled = list != null ? list.IrmEnabled.ToString() : string.Empty;

            recordList.Remarks = remarks;

            _LogHelper.AddRecordToCSV(recordList);
        }
    }
    public class ListAllSiteAllReportParameters
    {
        // Required parameters for the current report
        internal string SiteAdminUPN;
        // Optional parameters related to filter sites
        public bool RemoveAdmin { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool GroupIdDefined { get; set; } = false;
        // Optional parameters to filter lists
        public bool IncludeSystemLists { get; set; } = false;
        public bool IncludeResourceLists { get; set; } = false;
        public ListAllSiteAllReportParameters(string siteAdminUpn)
        {
            SiteAdminUPN = siteAdminUpn;
        }
    }
}